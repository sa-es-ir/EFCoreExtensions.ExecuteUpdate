using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CodeSample;

public class SampleDbContext : DbContext
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var connectionString = Environment.GetEnvironmentVariable("EF_SAMPLE_CONNECTION_STRING");

        optionsBuilder.UseSqlServer(connectionString);
    }

    public DbSet<Entities.Student> Students { get; set; } = null!;
}

public class SampleDbContextDesignFactory : IDesignTimeDbContextFactory<SampleDbContext>
{
    public SampleDbContext CreateDbContext(string[] args)
    {
        return new SampleDbContext();
    }
}