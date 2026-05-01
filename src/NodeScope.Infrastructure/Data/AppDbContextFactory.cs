using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NodeScope.Infrastructure.Data;

/// <summary>
/// Design-time factory enabling <c>dotnet ef migrations</c> workflows without composing the ASP.NET Core host assembly.
/// </summary>
/// <remarks>
/// Set <c>NODESCOPE_DESIGN_PG</c> to override the inferred PostgreSQL bootstrap connection string whenever local ports differ from defaults.
/// </remarks>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection =
            Environment.GetEnvironmentVariable("NODESCOPE_DESIGN_PG")
            ?? "Host=localhost;Port=5432;Database=nodescope;Username=nodescope;Password=changeme";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connection);

        return new AppDbContext(optionsBuilder.Options);
    }
}
