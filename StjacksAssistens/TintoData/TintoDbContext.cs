using Microsoft.EntityFrameworkCore;
using StjacksAssistens.ConfeccionData;
using StjacksAssistens.TintoModels;
using System.Text.RegularExpressions;

namespace StjacksAssistens.TintoData
{
    public class TintoDbContext : DbContext
    {
        // CORREGIDO: Debe usar DbContextOptions<TintoDbContext> para no confundirlo con el otro contexto
        public TintoDbContext(DbContextOptions<TintoDbContext> options)
            : base(options)
        {
        }

        public DbSet<Groups> Groups { get; set; }
        public DbSet<OperatorsTintos> OperatorsTintos { get; set; }
        public DbSet<PlanDelivery> PlanDelivery { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Aquí puedes agregar configuraciones adicionales si las necesitas
        }
    }
}
