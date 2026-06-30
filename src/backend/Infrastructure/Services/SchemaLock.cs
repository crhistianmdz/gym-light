// GymFlow Lite - Schema Versioning (HU-017)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using GymFlow.Domain.Interfaces;
using Npgsql;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Envoltorio de PostgreSQL Advisory Locks para control de concurrencia
/// durante operaciones de upgrade de esquema (HU-017).
///
/// Usa pg_try_advisory_lock() para adquirir locks de sesión que persisten
/// hasta que se liberan explícitamente o la conexión se cierra.
/// Esto garantiza que solo un proceso de upgrade se ejecute a la vez.
///
/// Lock ID por defecto: 1701 (HU-017 → 1701). Puede configurarse por instancia.
/// </summary>
public class SchemaLock : ISchemaLock
{
    /// <summary>
    /// Lock ID usado para pg_advisory_lock. Debe ser único a nivel de cluster.
    /// Cada instancia de GymFlow usa el mismo ID para evitar upgrades concurrentes.
    /// </summary>
    public const int DefaultLockId = 1701;

    private readonly string _connectionString;
    private readonly int _lockId;
    private NpgsqlConnection? _heldConnection;
    private readonly object _syncRoot = new();

    /// <param name="connectionString">Cadena de conexión a PostgreSQL.</param>
    /// <param name="lockId">ID del advisory lock. Default: 1701.</param>
    public SchemaLock(string connectionString, int lockId = DefaultLockId)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _lockId = lockId;
    }

    /// <summary>
    /// Intenta adquirir el advisory lock de sesión.
    /// 
    /// Abre una conexión dedicada y llama a pg_try_advisory_lock().
    /// La conexión debe mantenerse abierta mientras el lock esté tomado.
    /// 
    /// Retorna true si el lock fue adquirido, false si ya está tomado por otro proceso.
    /// </summary>
    public async Task<bool> AcquireAsync(CancellationToken ct = default)
    {
        lock (_syncRoot)
        {
            if (_heldConnection != null)
                throw new InvalidOperationException("El lock ya está adquirido. Liberar antes de readquirir.");
        }

        var conn = new NpgsqlConnection(_connectionString);
        try
        {
            await conn.OpenAsync(ct);

            await using var cmd = new NpgsqlCommand(
                "SELECT pg_try_advisory_lock(@lockId)",
                conn);
            cmd.Parameters.AddWithValue("@lockId", _lockId);

            var result = await cmd.ExecuteScalarAsync(ct);

            if (result is bool acquired && acquired)
            {
                lock (_syncRoot)
                {
                    _heldConnection = conn;
                }
                return true;
            }

            // Lock no adquirido: cerrar conexión
            await conn.CloseAsync();
            await conn.DisposeAsync();
            return false;
        }
        catch
        {
            await conn.DisposeAsync();
            throw;
        }
    }

    /// <summary>
    /// Libera el advisory lock previamente adquirido.
    /// Cierra la conexión dedicada automáticamente.
    /// 
    /// Es seguro llamarlo aunque el lock no esté tomado (no-op).
    /// </summary>
    public async Task ReleaseAsync(CancellationToken ct = default)
    {
        NpgsqlConnection? conn;
        lock (_syncRoot)
        {
            conn = _heldConnection;
            _heldConnection = null;
        }

        if (conn == null)
            return;

        try
        {
            // Si la conexión sigue abierta, liberar el lock explícitamente
            if (conn.State == System.Data.ConnectionState.Open)
            {
                await using var cmd = new NpgsqlCommand(
                    "SELECT pg_advisory_unlock(@lockId)",
                    conn);
                cmd.Parameters.AddWithValue("@lockId", _lockId);
                await cmd.ExecuteScalarAsync(ct);
            }
        }
        finally
        {
            await conn.CloseAsync();
            await conn.DisposeAsync();
        }
    }

    /// <summary>
    /// Verifica si el advisory lock está actualmente tomado.
    /// Abre una conexión temporal solo para verificar (no afecta al lock existente).
    /// </summary>
    public async Task<bool> IsHeldAsync(CancellationToken ct = default)
    {
        // Verificación rápida: si tenemos la conexión abierta, el lock está tomado por nosotros
        lock (_syncRoot)
        {
            if (_heldConnection != null)
                return true;
        }

        // Si no lo tenemos nosotros, preguntar a PostgreSQL
        await using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        // pg_try_advisory_lock intenta adquirir. Si falla, otro lo tiene.
        // Pero no queremos adquirirlo — solo verificar. Usamos una estrategia diferente:
        // Intentamos adquirir con NOWAIT-like behavior.
        // pg_try_advisory_lock es no bloqueante: retorna false si el lock está tomado.
        await using var cmd = new NpgsqlCommand(
            "SELECT pg_try_advisory_lock(@lockId)",
            conn);
        cmd.Parameters.AddWithValue("@lockId", _lockId);

        var result = await cmd.ExecuteScalarAsync(ct);
        var acquired = result is bool b && b;

        if (acquired)
        {
            // Lo adquirimos para testear. Liberarlo inmediatamente.
            await using var releaseCmd = new NpgsqlCommand(
                "SELECT pg_advisory_unlock(@lockId)",
                conn);
            releaseCmd.Parameters.AddWithValue("@lockId", _lockId);
            await releaseCmd.ExecuteScalarAsync(ct);

            // Si pudimos adquirirlo, significa que no estaba tomado
            return false;
        }

        // No pudimos adquirirlo → está tomado
        return true;
    }

    /// <summary>
    /// ID del lock configurado para esta instancia.
    /// </summary>
    public int LockId => _lockId;

    /// <summary>
    /// Indica si este proceso tiene actualmente el lock adquirido.
    /// </summary>
    public bool IsAcquired
    {
        get
        {
            lock (_syncRoot)
            {
                return _heldConnection != null;
            }
        }
    }
}
