// Copyright (C) 2026 GymFlow Lite Contributors
// AGPL v3.0 License (see LICENSE file for details)

using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for schema version tracking (HU-017).
/// Manages the schema_version table that records every applied migration.
/// </summary>
public class SchemaVersionRepository : ISchemaVersionRepository
{
    private readonly GymFlowDbContext _context;

    public SchemaVersionRepository(GymFlowDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task<SchemaVersion?> GetLatestVersion(CancellationToken ct = default)
    {
        return await _context.SchemaVersions
            .OrderByDescending(sv => sv.AppliedAt)
            .FirstOrDefaultAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SchemaVersion>> GetPendingBetween(
        string fromVersion,
        string toVersion,
        CancellationToken ct = default)
    {
        // This method returns versions that fall between fromVersion and toVersion.
        // Using simple string comparison that EF Core can translate to SQL.
        var all = await _context.SchemaVersions
            .OrderBy(sv => sv.Version)
            .ToListAsync(ct);
        
        return all
            .Where(sv => string.Compare(sv.Version, fromVersion, StringComparison.Ordinal) > 0
                      && string.Compare(sv.Version, toVersion, StringComparison.Ordinal) <= 0)
            .ToList();
    }

    /// <inheritdoc />
    public async Task RecordApplied(SchemaVersion entry, CancellationToken ct = default)
    {
        _context.SchemaVersions.Add(entry);
        await _context.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SchemaVersion>> GetByModule(
        string moduleName,
        CancellationToken ct = default)
    {
        return await _context.SchemaVersions
            .Where(sv => sv.ModuleName == moduleName)
            .OrderByDescending(sv => sv.AppliedAt)
            .ToListAsync(ct);
    }
}
