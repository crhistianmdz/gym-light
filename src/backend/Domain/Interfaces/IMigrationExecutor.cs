// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Abstraction for executing a single EF Core migration by name (HU-017).
///
/// Decouples SchemaUpgrader from EF Core internals so that unit tests
/// can mock migration execution without a real database.
///
/// The implementation delegates to EF Core's IMigrator.MigrateAsync()
/// which applies all pending migrations up to and including the named one.
/// When applied one-by-one in semver order, each call advances the DB
/// by exactly one migration because __EFMigrationsHistory tracks progress.
/// </summary>
public interface IMigrationExecutor
{
    /// <summary>
    /// Applies pending EF Core migrations up to and including the named migration.
    ///
    /// The migration name must match the EF Core migration ID (filename without .cs).
    /// Example: "20260611123000_AddSchemaVersionTable".
    /// </summary>
    /// <param name="migrationName">EF Core migration identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ExecuteAsync(string migrationName, CancellationToken ct = default);
}
