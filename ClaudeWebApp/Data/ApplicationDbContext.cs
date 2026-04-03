using Microsoft.EntityFrameworkCore;
using ClaudeWebApp.Models;

namespace ClaudeWebApp.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Sample> Samples { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Sample>()
            .HasQueryFilter(s => s.DeletedAt == null);
    }
}
