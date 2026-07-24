using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.SqlServer;
using StjacksAssistens.ConfeccionData;
using StjacksAssistens.TintoData;

namespace StjacksAssistens
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllersWithViews();
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddDbContext<TintoDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TintoConnection")));

            builder.Services.AddScoped<StjacksAssistens.Services.ReporteService>();

            var app = builder.Build();
      

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Homee}/{id?}");
              //pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
