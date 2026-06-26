using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Data;
using StjacksAssistens.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StjacksAssistens.Controllers
{
    public class OperatorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OperatorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =========================================================================
        // VISTA PRINCIPAL: LISTAR OPERARIOS Y GESTIONAR ASISTENCIA
        // =========================================================================
        public async Task<IActionResult> Index(int? periodId, int? categoryId)
        {
            var allPeriods = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();
            var allCategories = await _context.Set<Category>().ToListAsync();

            ViewBag.AllPeriods = allPeriods;
            ViewBag.Categories = allCategories;
            ViewBag.SelectedCategory = categoryId;

            // 2. Determinar periodo actual (Corrección del error de compilación p.PeriodId -> p.Id)
            Periodss? currentPeriod = null;
            if (periodId.HasValue)
            {
                currentPeriod = allPeriods.FirstOrDefault(p => p.Id == periodId.Value);
            }
            else
            {
                // Busca el activo, si no hay ninguno, toma el último de la lista
                currentPeriod = allPeriods.FirstOrDefault(p => p.IsActive == true) ?? allPeriods.FirstOrDefault();
            }

            var viewModel = new AttendanceViewModel
            {
                CurrentPeriod = currentPeriod,
                DaysInPeriod = new List<DateTime>(),
                Rows = new List<OperatorAttendanceRow>()
            };

            if (currentPeriod != null)
            {
                // Generar los días del periodo, excluyendo Sábados y Domingos
                for (var date = currentPeriod.StartDate; date <= currentPeriod.EndDate; date = date.AddDays(1))
                {
                    // Solo agregamos si NO es Sábado (Saturday) y NO es Domingo (Sunday)
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        viewModel.DaysInPeriod.Add(date);
                    }
                }

                // 3. CONSULTA: Traer operarios incluyendo sus relaciones de Área y Línea
                var query = _context.Set<Operators>()
                    .Include(o => o.Area)
                    .Include(o => o.Linea)
                    .AsQueryable();

                // Filtrado opcional por categoría (Área o Línea) si se presiona un filtro en la vista
                if (categoryId.HasValue)
                {
                    query = query.Where(o => o.AreaId == categoryId.Value || o.LineaId == categoryId.Value);
                }

                var operatorsList = await query.ToListAsync();

                // 4. Cargar las asistencias existentes de este periodo
                var attendances = await _context.Set<Attendence>()
                    .Where(a => a.PeriodId == currentPeriod.Id)
                    .ToListAsync();

                // 5. Construir las filas del ViewModel para la tabla
                foreach (var op in operatorsList)
                {
                    string areaName = op.Area?.Name ?? "Sin Área";
                    string lineaName = op.Linea != null ? $" - {op.Linea.Name}" : "";

                    var row = new OperatorAttendanceRow
                    {
                        OperatorsId = op.OperatorsId,
                        Code = op.Code,
                        Name = op.Name,
                        AreaId = op.AreaId,
                        LineaId = op.LineaId,
                        CategoryName = $"{areaName}{lineaName}"
                    };

                    // Llenar el estado de asistencia para cada día
                    foreach (var day in viewModel.DaysInPeriod)
                    {
                        var att = attendances.FirstOrDefault(a => a.OperatorsId == op.OperatorsId && a.AttendanceDate.Date == day.Date);
                        row.DailyStatus[day.Date] = att?.Status ?? "X"; // "X" por defecto si está vacío
                    }

                    viewModel.Rows.Add(row);
                }
            }

            return View(viewModel);
        }

        // =========================================================================
        // PROCESAR MODAL: CREAR NUEVO OPERARIO (Método Único y Optimizado)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int Code, string Name, int? AreaId, int? LineaId)
        {
            if (ModelState.IsValid)
            {
                var nuevoOperario = new Operators
                {
                    Code = Code,
                    Name = Name,
                    AreaId = AreaId,
                    LineaId = LineaId
                };

                // Validamos resguardo para CategoryId si sigue mapeado como obligatorio en BD
                if (AreaId.HasValue)
                {
                    nuevoOperario.CategoryId = AreaId.Value;
                }
                else if (LineaId.HasValue)
                {
                    nuevoOperario.CategoryId = LineaId.Value;
                }
                else
                {
                    var categoriaPorDefecto = await _context.Set<Category>().FirstOrDefaultAsync();
                    if (categoriaPorDefecto != null)
                    {
                        nuevoOperario.CategoryId = categoriaPorDefecto.Id;
                    }
                }

                _context.Add(nuevoOperario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // PROCESAR MODAL: EDICIÓN / ASIGNAR ÁREA Y LÍNEA MANUALMENTE
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int OperatorsId, int Code, string Name, int? AreaId, int? LineaId)
        {
            var operario = await _context.Set<Operators>().FindAsync(OperatorsId);
            if (operario == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                operario.Code = Code;
                operario.Name = Name;
                operario.AreaId = AreaId;   // Asignación manual del Área desde el modal
                operario.LineaId = LineaId; // Asignación manual de la Línea desde el modal

                if (AreaId.HasValue)
                {
                    operario.CategoryId = AreaId.Value;
                }

                _context.Update(operario);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================================================================
        // VISTA / REPORTE: AUSENTISMO DE OPERARIOS
        // =========================================================================
        public async Task<IActionResult> ReporteOperarios(int? periodId)
        {
            if (periodId == null || periodId == 0)
            {
                var ultimoPeriodo = await _context.Set<Periodss>().OrderByDescending(p => p.Id).FirstOrDefaultAsync();
                if (ultimoPeriodo == null) return NotFound("No hay periodos creados aún.");

                return RedirectToAction(nameof(ReporteOperarios), new { periodId = ultimoPeriodo.Id });
            }

            var periodo = await _context.Set<Periodss>().FindAsync(periodId);
            if (periodo == null) return NotFound();

            var todosLosPeriodos = await _context.Set<Periodss>().OrderByDescending(p => p.StartDate).ToListAsync();
            var operators = await _context.Set<Operators>().ToListAsync();
            var asistencias = await _context.Set<Attendence>()
                .Where(a => a.PeriodId == periodId && a.Status != "X")
                .ToListAsync();

            var listaEmpleados = new List<EmpleadoAusencia>();

            foreach (var op in operators)
            {
                // Corrección del error de compilación: op.Id -> op.OperatorsId
                var asistenciasOp = asistencias.Where(a => a.OperatorsId == op.OperatorsId).ToList();

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

        // Método auxiliar privado para formatear semanas en el reporte
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

        // =========================================================================
        // ACCIÓN AJAX: CAMBIAR ESTADO DE ASISTENCIA (Toggle en la cuadrícula)
        // =========================================================================
        //[HttpPost]
        //public async Task<IActionResult> UpdateAttendance(int operatorId, string date, string status, int periodId)
        //{
        //    var attendanceDate = DateTime.Parse(date).Date;
        //    var attendance = await _context.Set<Attendence>()
        //        .FirstOrDefaultAsync(a => a.OperatorsId == operatorId && a.AttendanceDate.Date == attendanceDate && a.PeriodId == periodId);

        //    if (attendance == null)
        //    {
        //        attendance = new Attendence
        //        {
        //            OperatorsId = operatorId,
        //            AttendanceDate = attendanceDate,
        //            Status = status,
        //            PeriodId = periodId
        //        };
        //        _context.Set<Attendence>().Add(attendance);
        //    }
        //    else
        //    {
        //        attendance.Status = status;
        //        _context.Update(attendance);
        //    }

        //    await _context.SaveChangesAsync();
        //    return Json(new { success = true });
        //}
        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(int operatorId, string date, string status, int periodId, string start = null, string end = null)
        {
            var attendanceDate = DateTime.Parse(date).Date;
            var attendance = await _context.Set<Attendence>()
                .FirstOrDefaultAsync(a => a.OperatorsId == operatorId && a.AttendanceDate.Date == attendanceDate && a.PeriodId == periodId);

            if (attendance == null)
            {
                attendance = new Attendence { OperatorsId = operatorId, AttendanceDate = attendanceDate, Status = status, PeriodId = periodId };
                _context.Set<Attendence>().Add(attendance);
            }
            else
            {
                attendance.Status = status;
            }

            // Guardar horas si no es "X"
            if (status != "X" && !string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
            {
                attendance.StartTime = TimeSpan.Parse(start);
                attendance.EndTime = TimeSpan.Parse(end);
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        // =========================================================================
        // PROCESAR MODALES: ACCIONES COMPLEMENTARIAS DE PERIODOS (Crear, Editar, Borrar)
        // =========================================================================
        [HttpPost]
        public async Task<IActionResult> CreatePeriod(string Description, DateTime StartDate, DateTime EndDate)
        {
            var newPeriod = new Periodss { Description = Description, StartDate = StartDate, EndDate = EndDate, IsActive = true };
            _context.Set<Periodss>().Add(newPeriod);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { periodId = newPeriod.Id });
        }

        [HttpPost]
        public async Task<IActionResult> EditPeriod(int id, string Description)
        {
            var period = await _context.Set<Periodss>().FindAsync(id);
            if (period != null)
            {
                period.Description = Description;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index), new { periodId = id });
        }

        public async Task<IActionResult> DeletePeriod(int id)
        {
            var period = await _context.Set<Periodss>().FindAsync(id);
            if (period != null)
            {
                var relatedAttendance = _context.Set<Attendence>().Where(a => a.PeriodId == id);
                _context.Set<Attendence>().RemoveRange(relatedAttendance);
                _context.Set<Periodss>().Remove(period);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}