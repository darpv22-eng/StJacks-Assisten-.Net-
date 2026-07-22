using Microsoft.EntityFrameworkCore;
using StjacksAssistens.ConfeccionData;
using StjacksAssistens.ConfeccionModels;

namespace StjacksAssistens.Services
{
    // Lógica compartida de los reportes de ausentismo.
    // Antes estaba duplicada entre OperatorsController y AusentismoController.
    public class ReporteService
    {
        private readonly ApplicationDbContext _context;

        public ReporteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task<Periodss?> ObtenerPeriodoAsync(int periodId) =>
            _context.Set<Periodss>().FirstOrDefaultAsync(p => p.Id == periodId);

        public Task<Periodss?> ObtenerUltimoPeriodoAsync() =>
            _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();

        public Task<List<Periodss>> ObtenerTodosLosPeriodosAsync() =>
            _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();

        // Reporte por día del área de Confección para un periodo dado.
        public async Task<OperariosReportViewModel> GenerarReporteConfeccionAsync(Periodss periodo)
        {
            var operators = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name.Contains("Confeccion"))
                .ToListAsync();

            var operatorIds = operators.Select(o => o.OperatorsId).ToList();

            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodo.Id && a.Status != "X" && operatorIds.Contains(a.OperatorsId))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                var asisOp = asistencias.Where(a => a.OperatorsId == op.OperatorsId).ToList();
                if (asisOp.Any())
                {
                    DateTime midPoint = periodo.StartDate.AddDays(7);
                    listaEmpleados.Add(new EmpleadoAusencia
                    {
                        Codigo = op.Code.ToString(),
                        Nombre = op.Name,
                        Semanas = new List<SemanaDetalle>
                        {
                            GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate < midPoint)),
                            GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate >= midPoint))
                        }
                    });
                }
            }

            return new OperariosReportViewModel
            {
                Periodo = periodo,
                Empleados = listaEmpleados,
                AreaNombre = "CONFECCIÓN P2",
                CDC = "407",
                TipoPlanilla = "06 Obra (Catorcenal)"
            };
        }

        public SemanaDetalle GenerarDetalleSemana(IEnumerable<Attendence> asistencias)
        {
            var detalle = new SemanaDetalle();
            var faltas = new List<string>();
            var obsBD = asistencias.FirstOrDefault(a => !string.IsNullOrEmpty(a.Observation))?.Observation;

            foreach (var a in asistencias)
            {
                switch (a.AttendanceDate.DayOfWeek)
                {
                    case DayOfWeek.Monday: detalle.Lunes = a.AttendanceDate; break;
                    case DayOfWeek.Tuesday: detalle.Martes = a.AttendanceDate; break;
                    case DayOfWeek.Wednesday: detalle.Miercoles = a.AttendanceDate; break;
                    case DayOfWeek.Thursday: detalle.Jueves = a.AttendanceDate; break;
                    case DayOfWeek.Friday: detalle.Viernes = a.AttendanceDate; break;
                }
                if (a.Status != "X") faltas.Add($"{a.Status}: {a.AttendanceDate:dd/MM}");
            }

            detalle.Motivo = !string.IsNullOrEmpty(obsBD) ? obsBD : string.Join(", ", faltas);
            return detalle;
        }
    }
}
