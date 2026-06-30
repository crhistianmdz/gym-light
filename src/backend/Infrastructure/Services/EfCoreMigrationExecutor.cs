// GymFlow Lite - Schema Versioning (HU-017)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Executes EF Core migrations one by one via IMigrator (HU-017).
///
/// Wraps EF Core's built-in migration infrastructure so that
/// SchemaUpgrader can apply individual migrations while recording
/// progress in the separate schema_version table.
///
/// IMigrator.MigrateAsync(target) applies all pending migrations from
/// the current state up to and including the target. Because
/// __EFMigrationsHistory is updated after each call, the one-by-one
/// loop in SchemaUpgrader advances correctly: each iteration applies
/// exactly one new migration.
/// </summary>
public class EfCoreMigrationExecutor : IMigrationExecutor
{
    private readonly GymFlowDbContext _context;

    public EfCoreMigrationExecutor(GymFlowDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <inheritdoc />
    public async Task ExecuteAsync(string migrationName, CancellationToken ct = default)
    {
        var migrator = _context.Database.GetInfrastructure()
            .GetRequiredService<IMigrator>();

        await migrator.MigrateAsync(migrationName, ct);
    }
}
