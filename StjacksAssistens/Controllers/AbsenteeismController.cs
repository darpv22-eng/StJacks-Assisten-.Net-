using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Data;
using StjacksAssistens.Models;

namespace StjacksAssistens.Controllers
{
    public class AusentismoController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AusentismoController(ApplicationDbContext context)
        {
            _context = context;
        }
        #region REPORTE CONFECCIÓN
        public async Task<IActionResult> ReporteOperarios(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                if (ultimo == null) return NotFound();
                return RedirectToAction(nameof(ReporteOperarios), new { periodId = ultimo.Id });
            }
            var periodo = await _context.Periodss.FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Periodss.OrderByDescending(p => p.StartDate).ToListAsync();
            var operators = await _context.Operators
                .Include(o => o.Category)
                .Where(o => o.Category.Name.Contains("Confeccion"))
                .ToListAsync();
            var asistencias = await _context.Attendence
                .Where(a => a.PeriodId == periodId && a.Status != "X")
                .ToListAsync();
            var listaEmpleados = new List<EmpleadoAusencia>();
            foreach (var op in operators)
            {
                var asisOp = asistencias.Where(a => a.OperatorsId == op.Id).ToList();
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
            return View(new OperariosReportViewModel
            {
                Periodo = periodo,
                Empleados = listaEmpleados,
                AreaNombre = "Confección P2",
                CDC = "407",
                TipoPlanilla = "06 Obra"
            });
        }
        private SemanaDetalle GenerarDetalleSemana(IEnumerable<Attendence> asistencias)
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
        #endregion
        #region REPORTE MECÁNICOS
        public async Task<IActionResult> ReporteMecanicos(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                return RedirectToAction(nameof(ReporteMecanicos), new { periodId = ultimo?.Id });
            }
            var periodo = await _context.Periodss.FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Periodss.OrderByDescending(p => p.StartDate).ToListAsync();
            var mecanicos = await _context.Operators
                .Include(o => o.Category)
                .Where(o => o.Category.Name == "Mecanicos")
                .ToListAsync();
            var asistencias = await _context.Attendence.Where(a => a.PeriodId == periodId).ToListAsync();
            var listaMecanicos = new List<EmpleadoAusentismoRow>();
            foreach (var mec in mecanicos)
            {
                var row = new EmpleadoAusentismoRow { Codigo = mec.Code, Nombre = mec.Name, Semanas = new List<SemanaDatos>() };
                for (int i = 0; i < 2; i++)
                {
                    DateTime inicioSemana = periodo.StartDate.AddDays(i * 7);
                    var asisSem = asistencias.Where(a => a.OperatorsId == mec.Id && a.AttendanceDate >= inicioSemana && a.AttendanceDate < inicioSemana.AddDays(7)).ToList();
                    row.Semanas.Add(new SemanaDatos
                    {
                        Lunes = asisSem.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Monday && a.Status == "PP")?.AttendanceDate,
                        Martes = asisSem.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Tuesday && a.Status == "PP")?.AttendanceDate,
                        Miercoles = asisSem.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Wednesday && a.Status == "PP")?.AttendanceDate,
                        Jueves = asisSem.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Thursday && a.Status == "PP")?.AttendanceDate,
                        Viernes = asisSem.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Friday && a.Status == "PP")?.AttendanceDate,
                        Motivo = asisSem.FirstOrDefault(a => !string.IsNullOrEmpty(a.Observation))?.Observation
                                 ?? string.Join(" / ", asisSem.Where(a => a.Status != "X").Select(a => a.Status).Distinct())
                    });
                }
                listaMecanicos.Add(row);
            }
            return View(new MecanicosReportViewModel { Periodo = periodo, Empleados = listaMecanicos });
        }
        #endregion
        [HttpPost]
        public async Task<IActionResult> GuardarObservacion([FromBody] ObservationRequest request)
        {
            var operario = await _context.Operators.FirstOrDefaultAsync(o => o.Code.ToString() == request.OperatorCode);
            if (operario == null) return NotFound();
            var asistencias = await _context.Attendence.Where(a => a.OperatorsId == operario.Id && a.PeriodId == request.PeriodId).ToListAsync();
            foreach (var item in asistencias) item.Observation = request.Observation;
            await _context.SaveChangesAsync();
            return Ok();
        }
        public async Task<IActionResult> ReporteMecanicosHoras(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                return RedirectToAction(nameof(ReporteMecanicosHoras), new { periodId = ultimo?.Id });
            }
            var periodo = await _context.Periodss.FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Periodss.OrderByDescending(p => p.StartDate).ToListAsync();
            var asistenciasConPP = await _context.Attendence
                .Where(a => a.PeriodId == periodId && a.Status == "PP")
                .ToListAsync();
            var idsMecanicosConFalta = asistenciasConPP.Select(a => a.OperatorsId).Distinct().ToList();
            var mecanicos = await _context.Operators
                .Include(o => o.Category)
                .Where(o => idsMecanicosConFalta.Contains(o.Id))
                .ToListAsync();
            var listaMecanicos = new List<EmpleadoAusentismoRow>();
            foreach (var mec in mecanicos)
            {
                var asisMec = await _context.Attendence
                    .Where(a => a.OperatorsId == mec.Id && a.PeriodId == periodId)
                    .ToListAsync();
                var registroConHoras = asisMec.FirstOrDefault(a => (a.Hours ?? 0) > 0 || (a.Minutes ?? 0) > 0);
                listaMecanicos.Add(new EmpleadoAusentismoRow
                {
                    Codigo = mec.Code,
                    Nombre = mec.Name,
                    HorasAusente = registroConHoras?.Hours ?? 0,
                    MinutosAusente = registroConHoras?.Minutes ?? 0,
                    Semanas = new List<SemanaDatos> {
                new SemanaDatos { 
                    Lunes = asisMec.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Monday && a.Status == "PP")?.AttendanceDate,
                    Martes = asisMec.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Tuesday && a.Status == "PP")?.AttendanceDate,
                    Miercoles = asisMec.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Wednesday && a.Status == "PP")?.AttendanceDate,
                    Jueves = asisMec.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Thursday && a.Status == "PP")?.AttendanceDate,
                    Viernes = asisMec.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Friday && a.Status == "PP")?.AttendanceDate,
                    Motivo = asisMec.FirstOrDefault(a => !string.IsNullOrEmpty(a.Observation))?.Observation ?? "PP"
                }
            }
                });
            }
            return View(new MecanicosReportViewModel { Periodo = periodo, Empleados = listaMecanicos });
        }
        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> GuardarHorasMecanico([FromBody] StjacksAssistens.Models.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");
            if (!int.TryParse(request.OperatorCode, out int codeInt))
                return BadRequest("El código del operario debe ser numérico.");
            var operario = await _context.Operators
                .FirstOrDefaultAsync(o => o.Code == codeInt);
            if (operario == null) return NotFound("Operario no encontrado.");
            var asistencias = await _context.Attendence
                .Where(a => a.OperatorsId == operario.Id && a.PeriodId == request.PeriodId)
                .ToListAsync();
            if (!asistencias.Any())
                return NotFound("No hay registros de asistencia para este periodo.");
            foreach (var item in asistencias)
            {
                item.Hours = request.Hours;
                item.Minutes = request.Minutes;
            }
            try
            {
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al guardar: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> GuardarObservacion([FromBody] StjacksAssistens.Models.ObservationRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos");
            var operario = await _context.Operators
                .FirstOrDefaultAsync(o => o.Code.ToString() == request.OperatorCode);
            if (operario == null) return NotFound();
            var asistencias = await _context.Attendence
                .Where(a => a.OperatorsId == operario.Id && a.PeriodId == request.PeriodId)
                .ToListAsync();
            foreach (var item in asistencias)
            {
                item.Observation = request.Observation;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }




        //#region REPORTE EMPAQUE
        //public async Task<IActionResult> ReporteEmpaque(int? periodId)
        //{
        //    if (periodId == null || periodId == 0)
        //    {
        //        var ultimo = await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
        //        if (ultimo == null) return NotFound();
        //        return RedirectToAction(nameof(ReporteEmpaque), new { periodId = ultimo.Id });
        //    }
        //    var periodo = await _context.Periodss.FindAsync(periodId);
        //    ViewBag.TodosLosPeriodos = await _context.Periodss.OrderByDescending(p => p.StartDate).ToListAsync();
        //    var operators = await _context.Operators
        //        .Include(o => o.Category)
        //        .Where(o => o.Category.Name.Contains("Empaque"))
        //        .ToListAsync();
        //    var asistencias = await _context.Attendence
        //        .Where(a => a.PeriodId == periodId && a.Status != "X")
        //        .ToListAsync();
        //    var listaEmpleados = new List<EmpleadoAusencia>();
        //    foreach (var op in operators)
        //    {
        //        var asisOp = asistencias.Where(a => a.OperatorsId == op.Id).ToList();
        //        if (asisOp.Any())
        //        {
        //            DateTime midPoint = periodo.StartDate.AddDays(7);
        //            listaEmpleados.Add(new EmpleadoAusencia
        //            {
        //                Codigo = op.Code.ToString(),
        //                Nombre = op.Name,
        //                Semanas = new List<SemanaDetalle>
        //        {
        //            GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate < midPoint)),
        //            GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate >= midPoint))
        //        }
        //            });
        //        }
        //    }
        //    return View(new OperariosReportViewModel
        //    {
        //        Periodo = periodo,
        //        Empleados = listaEmpleados,
        //        AreaNombre = "EMPAQUE / PRODUCTO TERMINADO",
        //        CDC = "410",
        //        TipoPlanilla = "06 Obra"
        //    });
        //}
        //#endregion
        #region REPORTE EMPAQUE

        // 1. VISTA POR DÍA (Ausentismo Normal)
        public async Task<IActionResult> ReporteEmpaque(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                if (ultimo == null) return NotFound();
                return RedirectToAction(nameof(ReporteEmpaque), new { periodId = ultimo.Id });
            }

            var periodo = await _context.Periodss.FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Periodss.OrderByDescending(p => p.StartDate).ToListAsync();

            // Filtro estricto: Solo personal de Empaque
            var operators = await _context.Operators
                .Include(o => o.Category)
                .Where(o => o.Category.Name.Contains("Empaque"))
                .ToListAsync();

            var asistencias = await _context.Attendence
                .Where(a => a.PeriodId == periodId && a.Status != "X")
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                var asisOp = asistencias.Where(a => a.OperatorsId == op.Id).ToList();
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

            return View(new OperariosReportViewModel
            {
                Periodo = periodo,
                Empleados = listaEmpleados,
                AreaNombre = "EMPAQUE / PRODUCTO TERMINADO",
                CDC = "410",
                TipoPlanilla = "06 Obra (Catorcenal)"
            });
        }

        // 2. VISTA POR HORAS (Descuentos)
        public async Task<IActionResult> ReporteEmpaqueHoras(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Periodss.OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                if (ultimo == null) return NotFound();
                return RedirectToAction(nameof(ReporteEmpaqueHoras), new { periodId = ultimo.Id });
            }

            var periodo = await _context.Periodss.FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Periodss.OrderByDescending(p => p.StartDate).ToListAsync();

            // Traemos asistencias con estado "PP" (Permiso Personal / Falta Parcial)
            var asistenciasConPP = await _context.Attendence
                .Where(a => a.PeriodId == periodId && a.Status == "PP")
                .ToListAsync();

            var idsEmpaqueConFalta = asistenciasConPP.Select(a => a.OperatorsId).Distinct().ToList();

            // Filtro estricto: Solo personal de Empaque que tenga faltas PP
            var operators = await _context.Operators
                .Include(o => o.Category)
                .Where(o => o.Category.Name.Contains("Empaque") && idsEmpaqueConFalta.Contains(o.Id))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                var asisOp = await _context.Attendence
                    .Where(a => a.OperatorsId == op.Id && a.PeriodId == periodId)
                    .ToListAsync();

                var registroConHoras = asisOp.FirstOrDefault(a => (a.Hours ?? 0) > 0 || (a.Minutes ?? 0) > 0);

                DateTime midPoint = periodo.StartDate.AddDays(7);

                listaEmpleados.Add(new EmpleadoAusencia
                {
                    Codigo = op.Code.ToString(),
                    Nombre = op.Name,
                    HorasAusente = registroConHoras?.Hours ?? 0,
                    MinutosAusente = registroConHoras?.Minutes ?? 0,
                    Semanas = new List<SemanaDetalle>
            {
                GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate < midPoint)),
                GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate >= midPoint))
            }
                });
            }

            return View(new OperariosReportViewModel
            {
                Periodo = periodo,
                Empleados = listaEmpleados,
                AreaNombre = "EMPAQUE / PRODUCTO TERMINADO",
                CDC = "410",
                TipoPlanilla = "06 Obra (Catorcenal)"
            });
        }

        // 3. ACCIÓN PARA GUARDAR HORAS DESDE LA VISTA DE EMPAQUE
        [HttpPost]
        public async Task<IActionResult> GuardarHorasEmpaque([FromBody] StjacksAssistens.Models.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");

            var operario = await _context.Operators.FirstOrDefaultAsync(o => o.Code.ToString() == request.OperatorCode);
            if (operario == null) return NotFound("Operario no encontrado.");

            var asistencias = await _context.Attendence
                .Where(a => a.OperatorsId == operario.Id && a.PeriodId == request.PeriodId)
                .ToListAsync();

            if (!asistencias.Any()) return NotFound("Sin registros de asistencia.");

            foreach (var item in asistencias)
            {
                item.Hours = request.Hours;
                item.Minutes = request.Minutes;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        #endregion
    }
}