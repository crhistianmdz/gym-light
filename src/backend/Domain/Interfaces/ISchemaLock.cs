// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Abstraction for PostgreSQL advisory lock acquisition and release (HU-017).
///
/// Enables unit testing of SchemaUpgrader without a real PostgreSQL instance
/// by mocking the lock acquisition behavior.
/// </summary>
public interface ISchemaLock
{
    /// <summary>ID of the advisory lock (maps to HU-017 → 1701).</summary>
    int LockId { get; }

    /// <summary>Whether this instance currently holds the lock.</summary>
    bool IsAcquired { get; }

    /// <summary>
    /// Attempts to acquire the advisory lock. Returns true if acquired,
    /// false if already held by another process.
    /// </summary>
    Task<bool> AcquireAsync(CancellationToken ct = default);

    /// <summary>
    /// Releases the advisory lock. Safe to call even if not held.
    /// </summary>
    Task ReleaseAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks whether the advisory lock is currently held by ANY session.
    /// </summary>
    Task<bool> IsHeldAsync(CancellationToken ct = default);
}
