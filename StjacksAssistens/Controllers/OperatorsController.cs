using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Data;
using StjacksAssistens.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StjacksAssistens.Controllers
{
    public class OperatorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OperatorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? periodId, int? categoryId)
        {
            var period = periodId.HasValue
                ? await _context.Periodss.FindAsync(periodId)
                : await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();

            ViewBag.Categories = await _context.Category.ToListAsync();
            ViewBag.AllPeriods = await _context.Periodss.ToListAsync();
            ViewBag.SelectedCategory = categoryId;

            if (period == null)
            {
                return View(new AttendanceViewModel
                {
                    CurrentPeriod = new Periodss { Description = "Sin Periodo" },
                    DaysInPeriod = new List<DateTime>(),
                    Rows = new List<OperatorAttendanceRow>()
                });
            }

            var days = new List<DateTime>();
            for (var dt = period.StartDate.Date; dt <= period.EndDate.Date; dt = dt.AddDays(1))
            {
                if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
                    days.Add(dt);
            }

            var query = _context.Operators.Include(o => o.Category).AsQueryable();
            if (categoryId.HasValue) query = query.Where(o => o.CategoryId == categoryId);
            var operators = await query.ToListAsync();

            var allAttendance = await _context.Attendence
                .Where(a => a.PeriodId == period.Id)
                .ToListAsync();

            var model = new AttendanceViewModel
            {
                CurrentPeriod = period,
                DaysInPeriod = days,
                Rows = operators.Select(o => new OperatorAttendanceRow
                {
                    OperatorsId = o.Id,
                    Code = o.Code,
                    Name = o.Name,
                    CategoryName = o.Category?.Name,
                    DailyStatus = days.ToDictionary(
                        d => d,
                        d => allAttendance.FirstOrDefault(a =>
                            a.OperatorsId == o.Id &&
                            a.AttendanceDate.Date == d.Date
                        )?.Status ?? "X"
                    )
                }).ToList()
            };

            ViewBag.CurrentPeriodId = period.Id;
            return View(model);
        }

       
        public async Task<IActionResult> ReporteOperarios(int? periodId) // 1. Cambiamos a int?
        {
            // 2. Insertamos la validación aquí al principio
            if (periodId == null || periodId == 0)
            {
                var ultimoPeriodo = await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                if (ultimoPeriodo == null) return NotFound("No hay periodos creados aún.");

                return RedirectToAction(nameof(ReporteOperarios), new { periodId = ultimoPeriodo.Id });
            }

            // 3. El resto de tu lógica continúa igual, usando el periodId validado
            var periodo = await _context.Periodss.FindAsync(periodId);
            if (periodo == null) return NotFound();

            var todosLosPeriodos = await _context.Periodss.OrderByDescending(p => p.StartDate).ToListAsync();

            // Obtenemos todos los operarios y su asistencia para este periodo
            var operators = await _context.Operators.ToListAsync();
            var asistencias = await _context.Attendence
                .Where(a => a.PeriodId == periodId && a.Status != "X")
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                var asistenciasOp = asistencias.Where(a => a.OperatorsId == op.Id).ToList();

                if (asistenciasOp.Any())
                {
                    DateTime midPoint = periodo.StartDate.AddDays(7);

                    var semanas = new List<SemanaDetalle>
            {
                GenerarDetalleSemana(asistenciasOp.Where(a => a.AttendanceDate < midPoint)),
                GenerarDetalleSemana(asistenciasOp.Where(a => a.AttendanceDate >= midPoint))
            };

                    listaEmpleados.Add(new EmpleadoAusencia
                    {
                        Codigo = op.Code.ToString(),
                        Nombre = op.Name,
                        Semanas = semanas
                    });
                }
            }

            var viewModel = new OperariosReportViewModel
            {
                Periodo = periodo,
                Empleados = listaEmpleados,
                AreaNombre = "Confeccion P2",
                CDC = "407",
                TipoPlanilla = "06 Obra"
            };

            ViewBag.TodosLosPeriodos = todosLosPeriodos;
            return View(viewModel);
        }
        // Función auxiliar para mapear fechas a columnas de lunes a viernes
        private SemanaDetalle GenerarDetalleSemana(IEnumerable<Attendence> asistencias)
        {
            var detalle = new SemanaDetalle();
            var motivos = new List<string>();

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
                if (!string.IsNullOrEmpty(a.Status) && a.Status != "X")
                    motivos.Add($"{a.Status}: {a.AttendanceDate:dd/MM}");
            }
            detalle.Motivo = string.Join(", ", motivos);
            return detalle;
        }

        // --- MÉTODOS EXISTENTES ---

        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(int operatorId, string date, string status, int periodId)
        {
            var attendanceDate = DateTime.Parse(date).Date;
            var attendance = await _context.Attendence
                .FirstOrDefaultAsync(a => a.OperatorsId == operatorId && a.AttendanceDate.Date == attendanceDate && a.PeriodId == periodId);

            if (attendance == null)
            {
                attendance = new Attendence
                {
                    OperatorsId = operatorId,
                    AttendanceDate = attendanceDate,
                    Status = status,
                    PeriodId = periodId
                };
                _context.Attendence.Add(attendance);
            }
            else
            {
                attendance.Status = status;
                _context.Update(attendance);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> CreatePeriod(string Description, DateTime StartDate, DateTime EndDate)
        {
            var newPeriod = new Periodss { Description = Description, StartDate = StartDate, EndDate = EndDate, IsActive = true };
            _context.Periodss.Add(newPeriod);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { periodId = newPeriod.Id });
        }

        [HttpPost]
        public async Task<IActionResult> EditPeriod(int id, string Description)
        {
            var period = await _context.Periodss.FindAsync(id);
            if (period != null)
            {
                period.Description = Description;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { periodId = id });
        }

        public async Task<IActionResult> DeletePeriod(int id)
        {
            var period = await _context.Periodss.FindAsync(id);
            if (period != null)
            {
                var relatedAttendance = _context.Attendence.Where(a => a.PeriodId == id);
                _context.Attendence.RemoveRange(relatedAttendance);
                _context.Periodss.Remove(period);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Create(Operators op)
        {
            _context.Add(op);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var op = await _context.Operators.FindAsync(id);
            if (op != null)
            {
                _context.Operators.Remove(op);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}