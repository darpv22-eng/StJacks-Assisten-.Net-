using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.ConfeccionData;
using StjacksAssistens.ConfeccionModels;
using StjacksAssistens.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StjacksAssistens.ConfeccionControllers
{
    public class AusentismoController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReporteService _reportes;

        public AusentismoController(ApplicationDbContext context, ReporteService reportes)
        {
            _context = context;
            _reportes = reportes;
        }

        #region REPORTE CONFECCIÓN

        // 1. REPORTE POR DÍA (Ausentismo Regular)
        public async Task<IActionResult> ReporteOperarios(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _reportes.ObtenerUltimoPeriodoAsync();
                if (ultimo == null) return NotFound("No hay periodos creados aún.");
                return RedirectToAction(nameof(ReporteOperarios), new { periodId = ultimo.Id });
            }

            var periodo = await _reportes.ObtenerPeriodoAsync(periodId.Value);
            if (periodo == null) return NotFound("Periodo no encontrado.");

            ViewBag.TodosLosPeriodos = await _reportes.ObtenerTodosLosPeriodosAsync();

            return View(await _reportes.GenerarReporteConfeccionAsync(periodo));
        }

        // 2. REPORTE POR HORAS (Faltas Parciales "PP")
        public async Task<IActionResult> ReporteOperariosHoras(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _reportes.ObtenerUltimoPeriodoAsync();
                if (ultimo == null) return NotFound("No hay periodos creados aún.");
                return RedirectToAction(nameof(ReporteOperariosHoras), new { periodId = ultimo.Id });
            }

            var periodo = await _reportes.ObtenerPeriodoAsync(periodId.Value);
            if (periodo == null) return NotFound("Periodo no encontrado.");

            ViewBag.TodosLosPeriodos = await _reportes.ObtenerTodosLosPeriodosAsync();

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

            // Una sola consulta con todas las asistencias del periodo (antes se consultaba dentro del foreach)
            var asistenciasPeriodo = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && idsConfeccionConFalta.Contains(a.OperatorsId))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                var asisOp = asistenciasPeriodo.Where(a => a.OperatorsId == op.OperatorsId).ToList();

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
                        _reportes.GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate < midPoint && a.Status == "PP")),
                        _reportes.GenerarDetalleSemana(asisOp.Where(a => a.AttendanceDate >= midPoint && a.Status == "PP"))
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarHorasConfeccion([FromBody] StjacksAssistens.ConfeccionModels.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");

            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code.ToString() == request.OperatorCode);
            if (operario == null) return NotFound("Operario no encontrado.");

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

        #region REPORTE MECÁNICOS
        public async Task<IActionResult> ReporteMecanicos(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _reportes.ObtenerUltimoPeriodoAsync();
                if (ultimo == null) return NotFound("No hay periodos creados aún.");
                return RedirectToAction(nameof(ReporteMecanicos), new { periodId = ultimo.Id });
            }

            var periodo = await _reportes.ObtenerPeriodoAsync(periodId.Value);
            if (periodo == null) return NotFound("Periodo no encontrado.");

            ViewBag.TodosLosPeriodos = await _reportes.ObtenerTodosLosPeriodosAsync();

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
                var ultimo = await _reportes.ObtenerUltimoPeriodoAsync();
                if (ultimo == null) return NotFound("No hay periodos creados aún.");
                return RedirectToAction(nameof(ReporteMecanicosHoras), new { periodId = ultimo.Id });
            }
            var periodo = await _reportes.ObtenerPeriodoAsync(periodId.Value);
            if (periodo == null) return NotFound("Periodo no encontrado.");
            ViewBag.TodosLosPeriodos = await _reportes.ObtenerTodosLosPeriodosAsync();
            var asistenciasConPP = await _context.Set<Attendence>()
                .Include(a => a.Operator).ThenInclude(o => o.Area)
                .Where(a => a.PeriodId == periodId && a.Status == "PP" && a.Operator.Area.Name == "Mecanicos")
                .ToListAsync();
            var idsMecanicosConFalta = asistenciasConPP.Select(a => a.OperatorsId).Distinct().ToList();
            var mecanicos = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name == "Mecanicos" && idsMecanicosConFalta.Contains(o.OperatorsId))
                .ToListAsync();
            var asistenciasPeriodo = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && idsMecanicosConFalta.Contains(a.OperatorsId))
                .ToListAsync();
            var listaMecanicos = new List<EmpleadoAusentismoRow>();
            foreach (var mec in mecanicos)
            {
                var asisMec = asistenciasPeriodo.Where(a => a.OperatorsId == mec.OperatorsId).ToList();
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarHorasMecanico([FromBody] StjacksAssistens.ConfeccionModels.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");
            if (!int.TryParse(request.OperatorCode, out int codeInt))
                return BadRequest("El código del operario debe ser numérico.");
            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code == codeInt);
            if (operario == null) return NotFound("Operario no encontrado.");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarObservacion([FromBody] StjacksAssistens.ConfeccionModels.ObservationRequest request)
        {
            if (request == null) return BadRequest("Datos inválidos");
            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code.ToString() == request.OperatorCode);
            if (operario == null) return NotFound();

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
        public async Task<IActionResult> ReporteEmpaque(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimo = await _reportes.ObtenerUltimoPeriodoAsync();
                if (ultimo == null) return NotFound("No hay periodos creados aún.");
                return RedirectToAction(nameof(ReporteEmpaque), new { periodId = ultimo.Id });
            }

            var periodo = await _reportes.ObtenerPeriodoAsync(periodId.Value);
            if (periodo == null) return NotFound("Periodo no encontrado.");

            ViewBag.TodosLosPeriodos = await _reportes.ObtenerTodosLosPeriodosAsync();

            // CORRECCIÓN: Cambiado de .Category a .Area
            var operariosEmpaque = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name == "Empaque")
                .ToListAsync();

            var idsEmpaque = operariosEmpaque.Select(o => o.OperatorsId).ToList();

            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && idsEmpaque.Contains(a.OperatorsId))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();
            var finSemana1 = periodo.StartDate.AddDays(6).Date;

            foreach (var op in operariosEmpaque)
            {
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
                var ultimo = await _reportes.ObtenerUltimoPeriodoAsync();
                if (ultimo == null) return NotFound("No hay periodos creados aún.");
                return RedirectToAction(nameof(ReporteEmpaqueHoras), new { periodId = ultimo.Id });
            }

            var periodo = await _reportes.ObtenerPeriodoAsync(periodId.Value);
            if (periodo == null) return NotFound("Periodo no encontrado.");

            ViewBag.TodosLosPeriodos = await _reportes.ObtenerTodosLosPeriodosAsync();

            // CORRECCIÓN: Cambiado de .Category a .Area
            var operariosEmpaque = await _context.Set<Operators>()
                .Include(o => o.Area)
                .Where(o => o.Area.Name == "Empaque")
                .ToListAsync();

            var idsEmpaque = operariosEmpaque.Select(o => o.OperatorsId).ToList();

            var asistenciasConPP = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && a.Status == "PP" && idsEmpaque.Contains(a.OperatorsId))
                .ToListAsync();

            var idsConFaltaConcreta = asistenciasConPP.Select(a => a.OperatorsId).Distinct().ToList();

            // Una sola consulta con todas las asistencias del periodo (antes se consultaba dentro del foreach)
            var asistenciasPeriodo = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && idsConFaltaConcreta.Contains(a.OperatorsId))
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();
            var finSemana1 = periodo.StartDate.AddDays(6).Date;

            foreach (var op in operariosEmpaque.Where(o => idsConFaltaConcreta.Contains(o.OperatorsId)))
            {
                var asisEmp = asistenciasPeriodo.Where(a => a.OperatorsId == op.OperatorsId).ToList();

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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GuardarHorasEmpaque([FromBody] StjacksAssistens.ConfeccionModels.TimeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperatorCode))
                return BadRequest("Datos incompletos.");

            if (!int.TryParse(request.OperatorCode, out int codeInt))
                return BadRequest("El código del operario debe ser numérico.");

            var operario = await _context.Set<Operators>().FirstOrDefaultAsync(o => o.Code == codeInt);
            if (operario == null) return NotFound("Operario no encontrado.");

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


        public async Task<IActionResult> SábanaConsolidada(int? periodId)
        {
            var periodo = periodId.HasValue
                ? await _reportes.ObtenerPeriodoAsync(periodId.Value)
                : await _reportes.ObtenerUltimoPeriodoAsync();

            if (periodo == null) return NotFound();

            // Traemos asistencias con los datos del operador y su línea
            var asistencias = await _context.Set<Attendence>()
                .Include(a => a.Operator).ThenInclude(o => o.Linea)
                .Where(a => a.PeriodId == periodo.Id)
                .ToListAsync();

            // Lista fija de tus 9 líneas
            var lineas = new[] { "21", "23", "25", "26", "32", "35", "36", "37", "38" };

            var consolidado = new List<AusentismoConsolidadoViewModel>();

            // Generar la matriz por fecha y línea
            var fechas = asistencias.Select(a => a.AttendanceDate.Date).Distinct().OrderBy(d => d);

            foreach (var fecha in fechas)
            {
                foreach (var linea in lineas)
                {
                    var att = asistencias.FirstOrDefault(a => a.AttendanceDate.Date == fecha.Date && a.Operator.Linea != null && a.Operator.Linea.Name.Contains(linea));

                    // Lógica de cálculo basada en tu Excel
                    double inc = (att?.Status == "INC") ? 8.5 : 0;
                    double per = (att?.Status == "PP") ? (att.Hours + (att.Minutes / 60.0)) ?? 0 : 0;
                    double cli = (att?.Status == "CLINICA") ? (att.Hours + (att.Minutes / 60.0)) ?? 0 : 0;
                    double iss = (att?.Status == "ISSS") ? (att.Hours + (att.Minutes / 60.0)) ?? 0 : 0;

                    double totalHoras = inc + per + cli + iss;

                    consolidado.Add(new AusentismoConsolidadoViewModel
                    {
                        Fecha = fecha,
                        Linea = linea,
                        Incapacidad = inc,
                        PermisoPersonal = per,
                        Clinica = cli,
                        ISSS = iss,
                        TotalMinutos = totalHoras * 60,
                        ModaParcial = (totalHoras > 0) ? 1 : 0,
                        ModaTotal = (totalHoras > 0) ? (totalHoras / 8.5) : 0
                    });
                }
            }
            return View(consolidado);
        }
    }
}
