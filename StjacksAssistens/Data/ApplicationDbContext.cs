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
        public DbSet<Periodss> Periodss { get; set; }
        public DbSet<Attendence> Attendence { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Operators>()
                .HasIndex(u => u.Code)
                .IsUnique();

            // Configuración de la relación corregida
            modelBuilder.Entity<Attendence>()
                .HasOne(a => a.Period) // Cambiado de Periodss a Period
                .WithMany(p => p.Attendances)
                .HasForeignKey(a => a.PeriodId);
        }
    }
}

