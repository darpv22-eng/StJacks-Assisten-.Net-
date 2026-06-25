using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Data;
using StjacksAssistens.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

        // 1. REPORTE POR DÍA (Ausentismo Regular)
        public async Task<IActionResult> ReporteOperarios(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                if (ultimo == null) return NotFound();
                return RedirectToAction(nameof(ReporteOperarios), new { periodId = ultimo.Id });
            }

            var periodo = await _context.Set<Periodss>().FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();

            // CORRECCIÓN: Filtrar por el área de Confección usando la nueva estructura
            var operators = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name.Contains("Confeccion"))
                .ToListAsync();

            // CORRECCIÓN: op.Id -> op.OperatorsId
            var operatorIds = operators.Select(o => o.OperatorsId).ToList();

            // Filtrar asistencias SOLO de los operarios de Confección de este periodo
            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && a.Status != "X" && operatorIds.Contains(a.OperatorsId))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                // CORRECCIÓN: op.Id -> op.OperatorsId
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

            return View(new OperariosReportViewModel
            {
                Periodo = periodo,
                Empleados = listaEmpleados,
                AreaNombre = "CONFECCIÓN P2",
                CDC = "407",
                TipoPlanilla = "06 Obra (Catorcenal)"
            });
        }

        // 2. REPORTE POR HORAS (Faltas Parciales "PP")
        public async Task<IActionResult> ReporteOperariosHoras(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                if (ultimo == null) return NotFound();
                return RedirectToAction(nameof(ReporteOperariosHoras), new { periodId = ultimo.Id });
            }

            var periodo = await _context.Set<Periodss>().FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();

            // CORRECCIÓN: Filtrar asistencias PP asegurando que pertenezcan a Confección usando .Area
            var asistenciasConPP = await _context.Set<Attendence>()
                .Include(a => a.Operator).ThenInclude(o => o.Area)
                .Where(a => a.PeriodId == periodId && a.Status == "PP" && a.Operator.Area.Name.Contains("Confeccion"))
                .ToListAsync();

            var idsConfeccionConFalta = asistenciasConPP.Select(a => a.OperatorsId).Distinct().ToList();

            var operators = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name.Contains("Confeccion") && idsConfeccionConFalta.Contains(o.OperatorsId))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                // CORRECCIÓN: op.Id -> op.OperatorsId
                var asisOp = await _context.Set<Attendence>()
                    .Where(a => a.OperatorsId == op.OperatorsId && a.PeriodId == periodId)
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
                        GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate < midPoint && a.Status == "PP")),
                        GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate >= midPoint && a.Status == "PP"))
                    }
                });
            }

            return View(new OperariosReportViewModel
            {
                Periodo = periodo,
                Empleados = listaEmpleados,
                AreaNombre = "CONFECCIÓN P2",
                CDC = "407",
                TipoPlanilla = "06 Obra (Catorcenal)"
            });
        }
        #endregion

        // 3. GUARDAR HORAS ESPECÍFICO PARA CONFECCIÓN
        [HttpPost]
        public async Task<IActionResult> GuardarHorasConfeccion([FromBody] StjacksAssistens.Models.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");

            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code.ToString() == request.OperatorCode);
            if (operario == null) return NotFound("Operario no encontrado.");

            // CORRECCIÓN: operario.Id -> operario.OperatorsId
            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.OperatorsId == operario.OperatorsId && a.PeriodId == request.PeriodId)
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


        #region REPORTE MECÁNICOS
        public async Task<IActionResult> ReporteMecanicos(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                return RedirectToAction(nameof(ReporteMecanicos), new { periodId = ultimo?.Id });
            }
            var periodo = await _context.Set<Periodss>().FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();

            // CORRECCIÓN: Filtrar mecánicos utilizando la relación .Area
            var mecanicos = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name == "Mecanicos")
                .ToListAsync();

            var asistencias = await _context.Set<Attendence>().Where(a => a.PeriodId == periodId).ToListAsync();
            var listaMecanicos = new List<EmpleadoAusentismoRow>();

            foreach (var mec in mecanicos)
            {
                var row = new EmpleadoAusentismoRow { Codigo = mec.Code, Nombre = mec.Name, Semanas = new List<SemanaDatos>() };
                for (int i = 0; i < 2; i++)
                {
                    DateTime inicioSemana = periodo.StartDate.AddDays(i * 7);
                    // CORRECCIÓN: mec.Id -> mec.OperatorsId
                    var asisSem = asistencias.Where(a => a.OperatorsId == mec.OperatorsId && a.AttendanceDate >= inicioSemana && a.AttendanceDate < inicioSemana.AddDays(7)).ToList();
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

        public async Task<IActionResult> ReporteMecanicosHoras(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                return RedirectToAction(nameof(ReporteMecanicosHoras), new { periodId = ultimo?.Id });
            }

            var periodo = await _context.Set<Periodss>().FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();

            // CORRECCIÓN: Filtrar asistencias PP vinculando con .Area en lugar de .Category
            var asistenciasConPP = await _context.Set<Attendence>()
                .Include(a => a.Operator).ThenInclude(o => o.Area)
                .Where(a => a.PeriodId == periodId && a.Status == "PP" && a.Operator.Area.Name == "Mecanicos")
                .ToListAsync();

            var idsMecanicosConFalta = asistenciasConPP.Select(a => a.OperatorsId).Distinct().ToList();

            var mecanicos = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name == "Mecanicos" && idsMecanicosConFalta.Contains(o.OperatorsId))
                .ToListAsync();

            var listaMecanicos = new List<EmpleadoAusentismoRow>();

            foreach (var mec in mecanicos)
            {
                // CORRECCIÓN: mec.Id -> mec.OperatorsId
                var asisMec = await _context.Set<Attendence>()
                    .Where(a => a.OperatorsId == mec.OperatorsId && a.PeriodId == periodId)
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
        public async Task<IActionResult> GuardarHorasMecanico([FromBody] StjacksAssistens.Models.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");
            if (!int.TryParse(request.OperatorCode, out int codeInt))
                return BadRequest("El código del operario debe ser numérico.");

            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code == codeInt);
            if (operario == null) return NotFound("Operario no encontrado.");

            // CORRECCIÓN: operario.Id -> operario.OperatorsId
            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.OperatorsId == operario.OperatorsId && a.PeriodId == request.PeriodId)
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
            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code.ToString() == request.OperatorCode);
            if (operario == null) return NotFound();

            // CORRECCIÓN: operario.Id -> operario.OperatorsId
            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.OperatorsId == operario.OperatorsId && a.PeriodId == request.PeriodId)
                .ToListAsync();

            foreach (var item in asistencias)
            {
                item.Observation = request.Observation;
            }
            await _context.SaveChangesAsync();
            return Ok();
        }

        #region REPORTE EMPAQUE

        // 1. REPORTE POR DÍA (Ausentismo Regular Empaque)
        public async Task<IActionResult> ReporteEmpaque(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                return RedirectToAction(nameof(ReporteEmpaque), new { periodId = ultimo?.Id });
            }

            var periodo = await _context.Set<Periodss>().FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();

            // CORRECCIÓN: Cambiado de .Category a .Area
            var operariosEmpaque = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name == "Empaque")
                .ToListAsync();

            // CORRECCIÓN: o.Id -> o.OperatorsId
            var idsEmpaque = operariosEmpaque.Select(o => o.OperatorsId).ToList();

            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && idsEmpaque.Contains(a.OperatorsId))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();
            var finSemana1 = periodo.StartDate.AddDays(6).Date;

            foreach (var op in operariosEmpaque)
            {
                // CORRECCIÓN: op.Id -> op.OperatorsId
                var asisOp = asistencias.Where(a => a.OperatorsId == op.OperatorsId).ToList();

                if (asisOp.Any(a => a.Status == "PP" || a.Status == "F" || !string.IsNullOrEmpty(a.Observation)))
                {
                    var asisSem1 = asisOp.Where(a => a.AttendanceDate.Date <= finSemana1).ToList();
                    var asisSem2 = asisOp.Where(a => a.AttendanceDate.Date > finSemana1).ToList();

                    listaEmpleados.Add(new EmpleadoAusencia
                    {
                        Codigo = op.Code.ToString(),
                        Nombre = op.Name,
                        Semanas = new List<SemanaDetalle>
                        {
                            new SemanaDetalle
                            {
                                Lunes = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Monday)?.AttendanceDate,
                                Martes = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Tuesday)?.AttendanceDate,
                                Miercoles = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Wednesday)?.AttendanceDate,
                                Jueves = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Thursday)?.AttendanceDate,
                                Viernes = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Friday)?.AttendanceDate,
                                Motivo = asisSem1.FirstOrDefault(a => !string.IsNullOrEmpty(a.Observation))?.Observation ?? ""
                            },
                            new SemanaDetalle
                            {
                                Lunes = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Monday)?.AttendanceDate,
                                Martes = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Tuesday)?.AttendanceDate,
                                Miercoles = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Wednesday)?.AttendanceDate,
                                Jueves = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Thursday)?.AttendanceDate,
                                Viernes = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Friday)?.AttendanceDate,
                                Motivo = asisSem2.FirstOrDefault(a => !string.IsNullOrEmpty(a.Observation))?.Observation ?? ""
                            }
                        }
                    });
                }
            }

            var model = new OperariosReportViewModel
            {
                CDC = "400",
                AreaNombre = "CONFECCIÓN Y EMPAQUE (EMPAQUE)",
                TipoPlanilla = "14 Días (Catorcenal)",
                Periodo = periodo,
                Empleados = listaEmpleados
            };

            return View(model);
        }

        // 2. REPORTE POR HORAS (Faltas Parciales Empaque)
        public async Task<IActionResult> ReporteEmpaqueHoras(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                return RedirectToAction(nameof(ReporteEmpaqueHoras), new { periodId = ultimo?.Id });
            }

            var periodo = await _context.Set<Periodss>().FindAsync(periodId);
            ViewBag.TodosLosPeriodos = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();

            // CORRECCIÓN: Cambiado de .Category a .Area
            var operariosEmpaque = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name == "Empaque")
                .ToListAsync();

            // CORRECCIÓN: o.Id -> o.OperatorsId
            var idsEmpaque = operariosEmpaque.Select(o => o.OperatorsId).ToList();

            var asistenciasConPP = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && a.Status == "PP" && idsEmpaque.Contains(a.OperatorsId))
                .ToListAsync();

            var idsConFaltaConcreta = asistenciasConPP.Select(a => a.OperatorsId).Distinct().ToList();

            var listaEmpleados = new List<EmpleadoAusencia>();
            var finSemana1 = periodo.StartDate.AddDays(6).Date;

            foreach (var op in operariosEmpaque.Where(o => idsConFaltaConcreta.Contains(o.OperatorsId)))
            {
                // CORRECCIÓN: op.Id -> op.OperatorsId
                var asisEmp = await _context.Set<Attendence>()
                    .Where(a => a.OperatorsId == op.OperatorsId && a.PeriodId == periodId)
                    .ToListAsync();

                var registroConHoras = asisEmp.FirstOrDefault(a => (a.Hours ?? 0) > 0 || (a.Minutes ?? 0) > 0);

                var asisSem1 = asisEmp.Where(a => a.AttendanceDate.Date <= finSemana1).ToList();
                var asisSem2 = asisEmp.Where(a => a.AttendanceDate.Date > finSemana1).ToList();

                listaEmpleados.Add(new EmpleadoAusencia
                {
                    Codigo = op.Code.ToString(),
                    Nombre = op.Name,
                    HorasAusente = registroConHoras?.Hours ?? 0,
                    MinutosAusente = registroConHoras?.Minutes ?? 0,
                    Semanas = new List<SemanaDetalle>
                    {
                        new SemanaDetalle
                        {
                            Lunes = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Monday && a.Status == "PP")?.AttendanceDate,
                            Martes = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Tuesday && a.Status == "PP")?.AttendanceDate,
                            Miercoles = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Wednesday && a.Status == "PP")?.AttendanceDate,
                            Jueves = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Thursday && a.Status == "PP")?.AttendanceDate,
                            Viernes = asisSem1.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Friday && a.Status == "PP")?.AttendanceDate,
                            Motivo = asisSem1.FirstOrDefault(a => !string.IsNullOrEmpty(a.Observation))?.Observation ?? ""
                        },
                        new SemanaDetalle
                        {
                            Lunes = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Monday && a.Status == "PP")?.AttendanceDate,
                            Martes = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Tuesday && a.Status == "PP")?.AttendanceDate,
                            Miercoles = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Wednesday && a.Status == "PP")?.AttendanceDate,
                            Jueves = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Thursday && a.Status == "PP")?.AttendanceDate,
                            Viernes = asisSem2.FirstOrDefault(a => a.AttendanceDate.DayOfWeek == DayOfWeek.Friday && a.Status == "PP")?.AttendanceDate,
                            Motivo = asisSem2.FirstOrDefault(a => !string.IsNullOrEmpty(a.Observation))?.Observation ?? ""
                        }
                    }
                });
            }

            var model = new OperariosReportViewModel
            {
                CDC = "400",
                AreaNombre = "CONFECCIÓN Y EMPAQUE (EMPAQUE)",
                TipoPlanilla = "14 Días (Catorcenal)",
                Periodo = periodo,
                Empleados = listaEmpleados
            };

            return View(model);
        }

        // 3. GUARDAR HORAS DE EMPAQUE 
        [HttpPost]
        public async Task<IActionResult> GuardarHorasEmpaque([FromBody] StjacksAssistens.Models.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");

            if (!int.TryParse(request.OperatorCode, out int codeInt))
                return BadRequest("El código del operario debe ser numérico.");

            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code == codeInt);
            if (operario == null) return NotFound("Operario no encontrado.");

            // CORRECCIÓN: operario.Id -> operario.OperatorsId
            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.OperatorsId == operario.OperatorsId && a.PeriodId == request.PeriodId)
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