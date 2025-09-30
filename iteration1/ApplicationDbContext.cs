using iteration1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace iteration1;

public sealed class ApplicationDbContext : IdentityDbContext<TopFiveUser>
{
    public DbSet<Resource> Resources { get; set; }
    
    public DbSet<Vote> Votes { get; set; }
    
    public DbSet<Category> Categories { get; set; }

    public ApplicationDbContext() => this.Database.EnsureCreated();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Filename=TopFive.db");
    }
}