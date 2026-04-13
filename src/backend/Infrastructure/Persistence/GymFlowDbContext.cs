using GymFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Infrastructure.Persistence;

/// <summary>
/// DbContext principal de GymFlow Lite.
/// Toda modificación de esquema requiere una migración EF Core formal (AGENTS.md §11).
/// </summary>
public class GymFlowDbContext : DbContext
{
    public GymFlowDbContext(DbContextOptions<GymFlowDbContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<BodyMeasurement> BodyMeasurements { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    public DbSet<Product> Products { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleLine> SaleLines { get; set; }

    // HU-07 — Congelamiento de membresías
    public DbSet<MembershipFreeze> MembershipFreezes { get; set; }

    public DbSet<WorkoutLog> WorkoutLogs { get; set; }
    public DbSet<Routine> Routines { get; set; }
public DbSet<RoutineExercise> RoutineExercises { get; set; }
    public DbSet<RoutineAssignment> RoutineAssignments { get; set; }
    public DbSet<ExerciseCatalog> ExerciseCatalogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // --- HU-11: Rutinas Digitales ---

        // WorkoutLog: UNIQUE index en ClientGuid (idempotencia)
        modelBuilder.Entity<WorkoutLog>()
            .HasIndex(w => w.ClientGuid)
            .IsUnique()
            .HasDatabaseName("IX_WorkoutLogs_ClientGuid");

        // RoutineExercise: CHECK constraint (ejercicio de catálogo O nombre personalizado)
        modelBuilder.Entity<RoutineExercise>()
            .ToTable(t => t.HasCheckConstraint(
                "CK_RoutineExercises_ExerciseOrCustomName",
                "\"ExerciseCatalogId\" IS NOT NULL OR \"CustomName\" IS NOT NULL"));

        // RoutineAssignment: evitar múltiples cascades desde Member
        modelBuilder.Entity<RoutineAssignment>()
            .HasOne(ra => ra.Member)
            .WithMany()
            .HasForeignKey(ra => ra.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // WorkoutLog: evitar múltiples cascades desde Member
        modelBuilder.Entity<WorkoutLog>()
            .HasOne(w => w.Member)
            .WithMany()
            .HasForeignKey(w => w.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        // Routine → AppUser (restricción para no cascadear borrando rutinas)
        modelBuilder.Entity<Routine>()
            .HasOne(r => r.CreatedBy)
            .WithMany()
            .HasForeignKey(r => r.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // RoutineAssignment → AppUser
        modelBuilder.Entity<RoutineAssignment>()
            .HasOne(ra => ra.AssignedBy)
            .WithMany()
            .HasForeignKey(ra => ra.AssignedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ExerciseCatalog → AppUser (nullable)
        modelBuilder.Entity<ExerciseCatalog>()
            .HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);
        // ── AppUser ───────────────────────────────────────────────────────────
        modelBuilder.Entity<AppUser>(b =>
        {
            b.ToTable("AppUsers");
            b.HasKey(u => u.Id);
            b.Property(u => u.Email).IsRequired().HasMaxLength(256);
            b.HasIndex(u => u.Email).IsUnique();
            b.Property(u => u.FullName).IsRequired().HasMaxLength(200);
            b.Property(u => u.PasswordHash).IsRequired();
            b.Property(u => u.Role).HasConversion<string>();
        });

        // ── RefreshToken ──────────────────────────────────────────────────────
        modelBuilder.Entity<RefreshToken>(b =>
        {
            b.ToTable("RefreshTokens");
            b.HasKey(rt => rt.Id);
            b.Property(rt => rt.TokenHash).IsRequired().HasMaxLength(512);
            b.HasIndex(rt => rt.TokenHash);
            b.HasOne(rt => rt.User)
             .WithMany()
             .HasForeignKey(rt => rt.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Member  ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Member>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.FullName).IsRequired().HasMaxLength(200);
            e.Property(m => m.PhotoWebPUrl).IsRequired().HasMaxLength(500);
            e.Property(m => m.Status).HasConversion<string>().IsRequired();
            e.Property(m => m.MembershipEndDate).IsRequired();
            e.Property(m => m.CreatedAt).IsRequired();
            e.Property(m => m.UpdatedAt).IsRequired();
        });

        // ── AccessLog ─────────────────────────────────────────────────────────
        modelBuilder.Entity<AccessLog>(e =>
        {
            e.HasKey(l => l.Id);

            // Índice UNIQUE sobre ClientGuid: garantiza idempotencia a nivel DB (RFC §4)
            e.HasIndex(l => l.ClientGuid)
             .IsUnique()
             .HasDatabaseName("IX_AccessLogs_ClientGuid_Unique");

            e.Property(l => l.ClientGuid).IsRequired();
            e.Property(l => l.Timestamp).IsRequired();
            e.Property(l => l.PerformedByUserId).IsRequired();
            e.Property(l => l.WasAllowed).IsRequired();
            e.Property(l => l.IsOffline).IsRequired();
            e.Property(l => l.DenialReason).HasMaxLength(300);

            e.HasOne(l => l.Member)
             .WithMany()
             .HasForeignKey(l => l.MemberId)
             .OnDelete(DeleteBehavior.Restrict);
        });
        // Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.HasIndex(p => p.Sku).IsUnique().HasFilter("\"Sku\" IS NOT NULL");
            entity.Property(p => p.Price).HasColumnType("decimal(18,2)");
            entity.ToTable(t => t.HasCheckConstraint("CK_Product_Stock", "\"Stock\" >= 0"));
        });

        // Sale
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.ClientGuid).IsUnique();
            entity.Property(s => s.Total).HasColumnType("decimal(18,2)");
            entity.HasMany(s => s.Lines)
                  .WithOne(l => l.Sale)
                  .HasForeignKey(l => l.SaleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // SaleLine
        modelBuilder.Entity<SaleLine>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Ignore(l => l.Subtotal);
            entity.HasOne(l => l.Product)
                  .WithMany()
                  .HasForeignKey(l => l.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ── MembershipFreeze (HU-07) ─────────────────────────────────────────────
        modelBuilder.Entity<MembershipFreeze>(e =>
        {
            e.ToTable("MembershipFreezes");
            e.HasKey(f => f.Id);
            e.Property(f => f.StartDate).IsRequired();
            e.Property(f => f.EndDate).IsRequired();
            e.Property(f => f.DurationDays).IsRequired();
            e.Property(f => f.CreatedByUserId).IsRequired();
            e.Property(f => f.CreatedAt).IsRequired();

            // Relación con Member
            e.HasOne(f => f.Member)
             .WithMany()
             .HasForeignKey(f => f.MemberId)
             .OnDelete(DeleteBehavior.Cascade);

            // Índice para consultas por socio + año (filtro frecuente HU-07)
            e.HasIndex(f => new { f.MemberId, f.StartDate })
             .HasDatabaseName("IX_MembershipFreezes_MemberId_StartDate");
        });
        // ── BodyMeasurement (HU-09) ─────────────────────────────────────────────
        modelBuilder.Entity<BodyMeasurement>(e =>
        {
            e.ToTable("BodyMeasurements");
            e.HasKey(b => b.Id);
            e.Property(b => b.WeightKg).HasColumnType("decimal(7,2)").IsRequired();
            e.Property(b => b.BodyFatPct).HasColumnType("decimal(5,2)").IsRequired();
            e.Property(b => b.ChestCm).HasColumnType("decimal(6,2)").IsRequired();
            e.Property(b => b.WaistCm).HasColumnType("decimal(6,2)").IsRequired();
            e.Property(b => b.HipCm).HasColumnType("decimal(6,2)").IsRequired();
            e.Property(b => b.ArmCm).HasColumnType("decimal(6,2)").IsRequired();
            e.Property(b => b.LegCm).HasColumnType("decimal(6,2)").IsRequired();
            e.Property(b => b.UnitSystem).HasConversion<string>().IsRequired();
            e.Property(b => b.ClientGuid).IsRequired().HasMaxLength(36);
            e.HasIndex(b => b.ClientGuid).IsUnique().HasDatabaseName("IX_BodyMeasurements_ClientGuid");
            e.HasIndex(b => new { b.MemberId, b.RecordedAt }).HasDatabaseName("IX_BodyMeasurements_MemberId_RecordedAt");
            e.HasOne<Member>().WithMany().HasForeignKey(b => b.MemberId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Payment>(e =>
        {
            e.ToTable("Payments");
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.ClientGuid).IsUnique().HasDatabaseName("IX_Payments_ClientGuid");
            e.HasIndex(p => p.Timestamp).HasDatabaseName("IX_Payments_Timestamp");
            e.HasIndex(p => new { p.MemberId, p.Timestamp }).HasDatabaseName("IX_Payments_MemberId_Timestamp");
            e.Property(p => p.Amount).HasColumnType("decimal(18,2)").IsRequired();
            e.Property(p => p.Category).HasConversion<int>().IsRequired();
            e.Property(p => p.Notes).HasMaxLength(500);
            e.HasOne<AppUser>().WithMany().HasForeignKey(p => p.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });
    }

    public DbSet<Payment> Payments { get; set; }
}
