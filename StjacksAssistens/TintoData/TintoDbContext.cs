using Microsoft.EntityFrameworkCore;
using StjacksAssistens.ConfeccionData;
using StjacksAssistens.TintoModels;
using System.Text.RegularExpressions;

namespace StjacksAssistens.TintoData
{
    public class TintoDbContext : DbContext
    {
        public TintoDbContext(DbContextOptions<ApplicationDbContext> options)
          : base(options)
        {
        }

        public DbSet<Groups> Groups { get; set; }
        public DbSet<OperatorsTinto> OperatorsTintos { get; set; }
        public DbSet<PlanDelivery> PlanDelivery { get; set; }
      

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
