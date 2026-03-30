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
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<AccessLog> AccessLogs => Set<AccessLog>();

    public DbSet<Product> Products { get; set; }
    public DbSet<Sale> Sales { get; set; }
    public DbSet<SaleLine> SaleLines { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
            entity.Property(l => l.Subtotal).HasColumnType("decimal(18,2)");
            entity.HasOne(l => l.Product)
                  .WithMany()
                  .HasForeignKey(l => l.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
