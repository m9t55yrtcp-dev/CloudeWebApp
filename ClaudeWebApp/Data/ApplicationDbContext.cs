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
}