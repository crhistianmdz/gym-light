using GymFlow.Application.UseCases.Access;
using GymFlow.Application.UseCases.Admin;
using GymFlow.Application.UseCases.BodyMeasurements;
using GymFlow.Application.UseCases.Members;
using GymFlow.Application.UseCases.Routines;
using GymFlow.Application.UseCases.ExerciseCatalog;
using GymFlow.Application.UseCases.WorkoutLogs;
using GymFlow.Application.UseCases;
using GymFlow.Application.Interfaces;
using GymFlow.Domain.Interfaces;
using GymFlow.Infrastructure.Persistence;
using GymFlow.Infrastructure.Persistence.Repositories;
using GymFlow.Infrastructure.Persistence.Seed;
using GymFlow.Infrastructure.Services;
using GymFlow.WebAPI.Extensions;
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
