using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.ConfeccionData;
using StjacksAssistens.ConfeccionModels;

namespace StjacksAssistens.Controllers
{
    public class HomeController : Controller
    {
        // Jornada laboral: 8.5 horas = 510 minutos. Es la base del cálculo de "moda"
        // en la sábana de ausentismo (una falta de día completo equivale a 1.00).
        private const double JornadaHoras = 8.5;
        private const double JornadaMinutos = 510.0;

        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Dashboard de ausentismo por fecha y línea (equivalente a la hoja
        // "AUSENTISMO POR LINEA" del Excel, pero con KPIs, mapa de calor y gráficos).
        public async Task<IActionResult> Aunsentismo()
        {
            var (periodo, filas) = await ConstruirConsolidadoAsync();
            if (periodo == null)
                return View(new AusentismoDashboardViewModel());

            var vm = new AusentismoDashboardViewModel
            {
                Periodo = periodo.Description,
                Filas = filas
            };

            // ---- KPIs ----
            vm.TotalMinutos = filas.Sum(f => f.TotalMinutos);
            vm.JornadasEquivalentes = vm.TotalMinutos / JornadaMinutos;
            vm.DiasConAusencia = filas.Where(f => f.TotalMinutos > 0).Select(f => f.Fecha).Distinct().Count();

            // ---- Reparto por categoría (en minutos) ----
            vm.PorCategoria = new Dictionary<string, double>
            {
                ["Incapacidad"] = filas.Sum(f => f.Incapacidad) * 60,
                ["Permiso Personal"] = filas.Sum(f => f.PermisoPersonal) * 60,
                ["Clínica"] = filas.Sum(f => f.Clinica) * 60,
                ["ISSS"] = filas.Sum(f => f.ISSS) * 60
            };
            var catTop = vm.PorCategoria.OrderByDescending(kv => kv.Value).First();
            if (catTop.Value > 0) { vm.CategoriaTop = catTop.Key; vm.CategoriaTopMinutos = catTop.Value; }

            // ---- Ranking por línea ----
            vm.PorLinea = filas.GroupBy(f => f.Linea)
                .Select(g => new SerieItem { Etiqueta = g.Key, Valor = g.Sum(x => x.TotalMinutos) })
                .OrderByDescending(s => s.Valor)
                .ToList();
            var lineaTop = vm.PorLinea.FirstOrDefault();
            if (lineaTop != null && lineaTop.Valor > 0) { vm.LineaTop = lineaTop.Etiqueta; vm.LineaTopMinutos = lineaTop.Valor; }

            // ---- Tendencia por día ----
            vm.PorDia = filas.GroupBy(f => f.Fecha)
                .OrderBy(g => g.Key)
                .Select(g => new SerieItem { Etiqueta = g.Key.ToString("dd-MMM"), Valor = g.Sum(x => x.TotalMinutos) })
                .ToList();

            // ---- Mapa de calor ----
            vm.Lineas = filas.Select(f => f.Linea).Distinct().ToList();
            vm.Dias = filas.Select(f => f.Fecha).Distinct().OrderBy(d => d).ToList();
            foreach (var f in filas)
                vm.Celdas[$"{f.Linea}|{f.Fecha:yyyy-MM-dd}"] = f.TotalMinutos;

            return View(vm);
        }

        // Exporta el consolidado a CSV (BOM UTF-8 para que Excel respete acentos).
        public async Task<IActionResult> AunsentismoCsv()
        {
            var (periodo, filas) = await ConstruirConsolidadoAsync();
            if (periodo == null) return NotFound();

            var sb = new System.Text.StringBuilder();
            sb.Append('﻿'); // BOM
            sb.AppendLine("FECHA;MES;LINEA;INCAPACIDAD;PERMISO PERSONAL;CLINICA;ISSS;TOTAL MINUTOS;CATEGORIAS AFECTADAS;JORNADAS EQUIVALENTES");
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            var es = System.Globalization.CultureInfo.GetCultureInfo("es-SV");
            foreach (var f in filas)
            {
                sb.AppendLine(string.Join(';', new[]
                {
                    f.Fecha.ToString("dd/MM/yyyy", inv),
                    f.Fecha.ToString("MMMM yyyy", es),
                    f.Linea,
                    f.Incapacidad.ToString("0.##", inv),
                    f.PermisoPersonal.ToString("0.##", inv),
                    f.Clinica.ToString("0.##", inv),
                    f.ISSS.ToString("0.##", inv),
                    f.TotalMinutos.ToString("0.#", inv),
                    f.ModaParcial.ToString(inv),
                    f.ModaTotal.ToString("0.00", inv)
                }));
            }

            var nombre = $"ausentismo_{periodo.Description}.csv".Replace(' ', '_');
            return File(System.Text.Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", nombre);
        }

        // Arma las filas del consolidado (compartido por el dashboard y el CSV).
        private async Task<(Periodss? periodo, List<AusentismoConsolidadoViewModel> filas)> ConstruirConsolidadoAsync()
        {
            var periodo = await _context.Set<Periodss>()
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            var filas = new List<AusentismoConsolidadoViewModel>();
            if (periodo == null) return (null, filas);

            // Solo las líneas de producción (categorías con nombre numérico: 21, 23, 25...).
            var lineas = (await _context.Set<Category>().ToListAsync())
                .Where(c => int.TryParse(c.Name, out _))
                .OrderBy(c => int.Parse(c.Name))
                .ToList();

            var asistencias = await _context.Set<Attendence>()
                .Include(a => a.Operator).ThenInclude(o => o.Linea)
                .Where(a => a.PeriodId == periodo.Id && a.Status != "X")
                .ToListAsync();

            for (var dia = periodo.StartDate.Date; dia <= periodo.EndDate.Date; dia = dia.AddDays(1))
            {
                if (dia.DayOfWeek == DayOfWeek.Saturday || dia.DayOfWeek == DayOfWeek.Sunday) continue;

                foreach (var linea in lineas)
                {
                    var registros = asistencias
                        .Where(a => a.AttendanceDate.Date == dia && a.Operator?.Linea?.Name == linea.Name)
                        .ToList();

                    double incapacidad = HorasAusentes(registros, "INC");
                    double permiso = HorasAusentes(registros, "PP");
                    double clinica = HorasAusentes(registros, "CLINICA");
                    double isss = HorasAusentes(registros, "ISSS");
                    double totalMinutos = (incapacidad + permiso + clinica + isss) * 60;

                    filas.Add(new AusentismoConsolidadoViewModel
                    {
                        Fecha = dia,
                        Linea = linea.Name,
                        Incapacidad = incapacidad,
                        PermisoPersonal = permiso,
                        Clinica = clinica,
                        ISSS = isss,
                        TotalMinutos = totalMinutos,
                        ModaParcial = new[] { incapacidad, permiso, clinica, isss }.Count(v => v > 0),
                        ModaTotal = totalMinutos / JornadaMinutos
                    });
                }
            }
            return (periodo, filas);
        }
        private static double HorasAusentes(IEnumerable<Attendence> registros, string status) =>
            registros
                .Where(a => a.Status == status)
                .Sum(a => (a.Hours.HasValue || a.Minutes.HasValue)
                    ? (a.Hours ?? 0) + ((a.Minutes ?? 0) / 60.0)
                    : JornadaHoras);

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Homee()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }
        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
