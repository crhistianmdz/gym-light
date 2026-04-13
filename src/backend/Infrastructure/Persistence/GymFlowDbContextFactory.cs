using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GymFlow.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for EF Core CLI tools (dotnet ef migrations add, etc.).
/// Uses a local Postgres connection — only active during development/migration generation.
/// </summary>
public class GymFlowDbContextFactory : IDesignTimeDbContextFactory<GymFlowDbContext>
{
    public GymFlowDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=gymflowdb;Username=gymflow;Password=gymflow123";

        var optionsBuilder = new DbContextOptionsBuilder<GymFlowDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        return new GymFlowDbContext(optionsBuilder.Options);
    }
}
