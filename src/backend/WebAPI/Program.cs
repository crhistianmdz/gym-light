using GymFlow.Application.UseCases.Access;
using GymFlow.Application.UseCases.Admin;
using GymFlow.Application.UseCases.BodyMeasurements;
using GymFlow.Application.UseCases.Members;
using GymFlow.Application.UseCases.Routines;
using GymFlow.Application.UseCases.ExerciseCatalog;
using GymFlow.Application.UseCases.WorkoutLogs;
using GymFlow.Application.UseCases;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Persistence.Repositories;
using GymFlow.Infrastructure.Persistence.Seed;
using GymFlow.Infrastructure.Services;
using GymFlow.Application.UseCases.Schema;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence.Repositories;
using GymFlow.Infrastructure.Persistence.Services;
using GymFlow.Infrastructure.Services;
using GymFlow.WebAPI.Extensions;
using GymFlow.WebAPI.Plugins;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<GymFlowDbContext>(opts =>
    opts.UseNpgsql(connectionString));

// ── Auth (JWT + use cases from AuthExtensions) ────────────────────────────────
builder.Services.AddGymFlowAuth(builder.Configuration);

    // ── Repositories ──────────────────────────────────────────────────────────────
    builder.Services.AddScoped<IPhotoStorageService>(sp => new LocalPhotoStorageService(sp.GetRequiredService<IWebHostEnvironment>().WebRootPath));
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<IAccessLogRepository, AccessLogRepository>();
builder.Services.AddScoped<IBodyMeasurementRepository, BodyMeasurementRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IPluginRegistryRepository, PluginRegistryRepository>();
builder.Services.AddScoped<GymFlow.WebAPI.Filters.IdempotencyFilter>();

builder.Services.AddSingleton<IPluginLoader, PluginLoader>();

// ── Schema Versioning (HU-017) ───────────────────────────────────────────────
builder.Services.AddScoped<ISchemaVersionRepository, SchemaVersionRepository>();
builder.Services.AddSingleton<ISchemaMetadata>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    return new SchemaMetadataService(connectionString);
});

// Schema infrastructure services (direct construction with connection string)
builder.Services.AddSingleton<ISchemaLock>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    return new SchemaLock(connectionString);
});

builder.Services.AddSingleton<BackupHelper>(sp =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;
    return new BackupHelper(connectionString);
});

builder.Services.AddScoped<IMigrationExecutor, EfCoreMigrationExecutor>();
builder.Services.AddSingleton<MigrationPolicy>();
builder.Services.AddScoped<SchemaUpgrader>();

// Schema use cases
builder.Services.AddScoped<UpgradeSchemaUseCase>();
builder.Services.AddScoped<GetSchemaStatusUseCase>();
builder.Services.AddScoped<ValidateSchemaUseCase>();

// ── Use Cases ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<ValidateAccessUseCase>();
builder.Services.AddScoped<RegisterMemberUseCase>();
builder.Services.AddScoped<AddBodyMeasurementUseCase>();
builder.Services.AddScoped<GetBodyMeasurementsUseCase>();
builder.Services.AddScoped<RegisterPaymentUseCase>();
builder.Services.AddScoped<GetIncomeReportUseCase>();
builder.Services.AddScoped<GetChurnReportUseCase>();
builder.Services.AddScoped<FreezeMembershipUseCase>();
builder.Services.AddScoped<UnfreezeMembershipUseCase>();
builder.Services.AddScoped<CancelMembershipUseCase>();
builder.Services.AddScoped<CreateRoutineUseCase>();
builder.Services.AddScoped<GetRoutinesUseCase>();
builder.Services.AddScoped<AssignRoutineUseCase>();
builder.Services.AddScoped<UpdateRoutineUseCase>();
builder.Services.AddScoped<GetMemberRoutinesUseCase>();
builder.Services.AddScoped<CreateExerciseUseCase>();
builder.Services.AddScoped<GetExercisesUseCase>();
builder.Services.AddScoped<CreateWorkoutLogUseCase>();
builder.Services.AddScoped<GetWorkoutLogsUseCase>();
builder.Services.AddScoped<GetSalesUseCase>();

// ── MVC + Swagger ─────────────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
    opts.AddDefaultPolicy(p =>
        p.WithOrigins(
                builder.Configuration["Cors:AllowedOrigins"]?.Split(',')
                ?? ["http://localhost:5173"])
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials()));

var app = builder.Build();

// ── Migrations + Seed ─────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<GymFlowDbContext>();
    if (app.Environment.IsDevelopment())
    {
        app.Logger.LogInformation("Development: applying EF Core migrations...");
        await db.Database.MigrateAsync();
        app.Logger.LogInformation("Migrations applied. Running seed...");
        await ProductSeeder.SeedAsync(db);
        app.Logger.LogInformation("Seed complete.");
    }

    // ── Plugin Discovery (HU-015) ─────────────────────────────────────────────────────
    var pluginLoader = scope.ServiceProvider.GetRequiredService<IPluginLoader>();
    var pluginRegistryRepo = scope.ServiceProvider.GetRequiredService<IPluginRegistryRepository>();
    var pluginsPath = builder.Configuration["Plugins:Path"] ?? "/plugins";

    app.Logger.LogInformation("Discovering plugins from: {Path}", pluginsPath);
    var discoveredPlugins = await pluginLoader.DiscoverAsync(pluginsPath);

    foreach (var discovered in discoveredPlugins)
    {
        var plugin = PluginRegistry.Create(
            discovered.Metadata.Id,
            discovered.Metadata.Name,
            discovered.Metadata.Version,
            discovered.Metadata.OfflineCapable);
        await pluginRegistryRepo.UpsertAsync(plugin, CancellationToken.None);
        app.Logger.LogInformation("Registered plugin: {Id} v{Version}", plugin.Id, plugin.Version);
    }

    // Register plugin services for enabled plugins
    var enabledPlugins = await pluginRegistryRepo.GetEnabledAsync(CancellationToken.None);
    foreach (var enabledPlugin in enabledPlugins)
    {
        var discovered = discoveredPlugins.FirstOrDefault(d => d.Metadata.Id == enabledPlugin.Id);
        if (discovered != null)
        {
            pluginLoader.RegisterServices(builder.Services, discovered.Instance);
            app.Logger.LogInformation("Enabled plugin services: {Id}", enabledPlugin.Id);
        }
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
