using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Models;

namespace StjacksAssistens.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
          : base(options)
        {
        }

        public DbSet<Operators> Operators { get; set; }
        public DbSet<Category> Category { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Operators>()
                .HasIndex(u => u.Code)
                .IsUnique();
        }
    }
}

