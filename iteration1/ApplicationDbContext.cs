using iteration1.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace iteration1;

public sealed class ApplicationDbContext : IdentityDbContext<TopFiveUser>
{
    private readonly bool _isTestMode;

    public DbSet<Resource> Resources { get; set; }

    public DbSet<Vote> Votes { get; set; }

    public DbSet<Category> Categories { get; set; }

    public DbSet<Section> Sections { get; set; }

    public ApplicationDbContext()
    {
        _isTestMode = false;
        this.Database.EnsureCreated();
    }

    // Constructor for testing with in-memory database
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
        _isTestMode = true;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Only configure SQLite if not already configured (i.e., not in test mode)
        if (!optionsBuilder.IsConfigured && !_isTestMode)
        {
            optionsBuilder.UseSqlite("Filename=TopFive.db");
        }

        // Suppress transaction warnings for in-memory database during testing
        if (_isTestMode)
        {
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.AmbientTransactionWarning));
        }
    }
}