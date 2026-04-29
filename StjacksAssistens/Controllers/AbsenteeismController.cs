using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Data;
using StjacksAssistens.Models;

namespace StjacksAssistens.Controllers
{

    //Planilla de mecanicos 
    public class AusentismoController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AusentismoController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> ReporteMecanicos(int periodId)
        {
            var periodo = await _context.Periodss.FindAsync(periodId);
            if (periodo == null) return NotFound();
            ViewBag.TodosLosPeriodos = await _context.Periodss
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
            var mecanicos = await _context.Operators
                .Include(o => o.Category)
                .Where(o => o.Category.Name == "Mecanicos")
                .ToListAsync();

            var asistencias = await _context.Attendence
                .Where(a => a.PeriodId == periodId)
                .ToListAsync();

            var model = new MecanicosReportViewModel
            {
                Periodo = periodo,
                Empleados = new List<EmpleadoAusentismoRow>()
            };

            foreach (var mec in mecanicos)
            {
                var row = new EmpleadoAusentismoRow
                {
                    Codigo = mec.Code,
                    Nombre = mec.Name,
                    Semanas = new List<SemanaDatos>()
                };

                for (int i = 0; i < 2; i++)
                {
                    DateTime inicioSemana = periodo.StartDate.AddDays(i * 7);
                    var asistenciasSemana = asistencias.Where(a => a.OperatorsId == mec.Id).ToList();

                    var semana = new SemanaDatos();
                    semana.Lunes = GetFechaSiEsPP(inicioSemana, DayOfWeek.Monday, asistenciasSemana);
                    semana.Martes = GetFechaSiEsPP(inicioSemana, DayOfWeek.Tuesday, asistenciasSemana);
                    semana.Miercoles = GetFechaSiEsPP(inicioSemana, DayOfWeek.Wednesday, asistenciasSemana);
                    semana.Jueves = GetFechaSiEsPP(inicioSemana, DayOfWeek.Thursday, asistenciasSemana);
                    semana.Viernes = GetFechaSiEsPP(inicioSemana, DayOfWeek.Friday, asistenciasSemana);

                    var motivos = asistenciasSemana
                        .Where(a => a.AttendanceDate >= inicioSemana && a.AttendanceDate < inicioSemana.AddDays(7) && a.Status != "X")
                        .Select(a => a.Observation ?? a.Status)
                        .Distinct()
                        .ToList();

                    semana.Motivo = string.Join(" / ", motivos);
                    row.Semanas.Add(semana);
                }
                model.Empleados.Add(row);
            }

            return View(model);
        }

        private DateTime? GetFechaSiEsPP(DateTime inicioSemana, DayOfWeek diaBuscado, List<Attendence> lista)
        {
            var fecha = inicioSemana;
            while (fecha.DayOfWeek != diaBuscado) fecha = fecha.AddDays(1);

            var registro = lista.FirstOrDefault(a => a.AttendanceDate.Date == fecha.Date);
            return (registro != null && registro.Status == "PP") ? registro.AttendanceDate : null;
        }
    }
}