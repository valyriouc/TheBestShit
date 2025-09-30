using iteration1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace iteration1;

public sealed class ApplicationDbContext : IdentityDbContext<TopFiveUser, TopFiveRole, uint>
{
    public DbSet<Resource> Resources { get; set; }
    
    public DbSet<Vote> Votes { get; set; }
    
    public DbSet<Category> Categories { get; set; }
}