using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.ConfeccionData;
using StjacksAssistens.ConfeccionModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace StjacksAssistens.Controllers
{
    public class ConfeccionOperatorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ConfeccionOperatorsController(ApplicationDbContext context)
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
            Periodss? currentPeriod = null;
            if (periodId.HasValue)
            {
                currentPeriod = allPeriods.FirstOrDefault(p => p.Id == periodId.Value);
            }
            else
            {
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
                for (var date = currentPeriod.StartDate; date <= currentPeriod.EndDate; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                    {
                        viewModel.DaysInPeriod.Add(date);
                    }
                }
                var query = _context.Set<Operators>()
                    .Include(o => o.Area)
                    .Include(o => o.Linea)
                    .AsQueryable();
                if (categoryId.HasValue)
                {
                    query = query.Where(o => o.AreaId == categoryId.Value || o.LineaId == categoryId.Value);
                }

                var operatorsList = await query.ToListAsync();
                var attendances = await _context.Set<Attendence>()
                    .Where(a => a.PeriodId == currentPeriod.Id)
                    .ToListAsync();
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
                    foreach (var day in viewModel.DaysInPeriod)
                    {
                        var att = attendances.FirstOrDefault(a => a.OperatorsId == op.OperatorsId && a.AttendanceDate.Date == day.Date);
                        row.DailyStatus[day.Date] = att?.Status ?? "X";
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
                operario.AreaId = AreaId;
                operario.LineaId = LineaId;

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
        // La lógica vive ahora en AusentismoController + ReporteService; esta ruta
        // se conserva por compatibilidad (aquí nunca existió la vista .cshtml).
        // =========================================================================
        public IActionResult ReporteOperarios(int? periodId)
        {
            return RedirectToAction("ReporteOperarios", "Ausentismo", new { periodId });
        }

        // =========================================================================
        // ACCIÓN AJAX: CAMBIAR ESTADO DE ASISTENCIA (Toggle en la cuadrícula)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAttendance(int operatorId, DateTime date, string status, int periodId, string? start, string? end)
        {
            var attendance = await _context.Attendence
                .FirstOrDefaultAsync(a => a.OperatorsId == operatorId && a.AttendanceDate.Date == date.Date && a.PeriodId == periodId);

            if (attendance == null)
            {
                attendance = new Attendence
                {
                    OperatorsId = operatorId,
                    AttendanceDate = date,
                    PeriodId = periodId,
                    Status = status
                };
                _context.Attendence.Add(attendance);
            }
            else
            {
                attendance.Status = status;
            }
            if (!string.IsNullOrEmpty(start) && TimeSpan.TryParse(start, out var startTime)) attendance.StartTime = startTime;
            if (!string.IsNullOrEmpty(end) && TimeSpan.TryParse(end, out var endTime)) attendance.EndTime = endTime;

            if (attendance.StartTime.HasValue && attendance.EndTime.HasValue)
            {
                var diff = attendance.EndTime.Value - attendance.StartTime.Value;
                attendance.Hours = (int)diff.TotalHours;
                attendance.Minutes = diff.Minutes;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
        // =========================================================================
        // PROCESAR MODALES: ACCIONES COMPLEMENTARIAS DE PERIODOS (Crear, Editar, Borrar)
        // =========================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePeriod(string Description, DateTime StartDate, DateTime EndDate)
        {
            var newPeriod = new Periodss { Description = Description, StartDate = StartDate, EndDate = EndDate, IsActive = true };
            _context.Set<Periodss>().Add(newPeriod);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { periodId = newPeriod.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePeriod(int id)
        {
            try
            {
                var period = await _context.Set<Periodss>().FindAsync(id);
                if (period != null)
                {
                    var relatedAttendance = _context.Set<Attendence>().Where(a => a.PeriodId == id);
                    if (relatedAttendance.Any())
                    {
                        _context.Set<Attendence>().RemoveRange(relatedAttendance);
                        await _context.SaveChangesAsync();
                    }
                    _context.Set<Periodss>().Remove(period);
                    await _context.SaveChangesAsync();
                }
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Ok();
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo eliminar el periodo porque tiene registros dependientes asociados.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var operario = await _context.Set<Operators>().FindAsync(id);
            if (operario != null)
            {
                var relatedAttendance = _context.Set<Attendence>().Where(a => a.OperatorsId == id);
                if (relatedAttendance.Any())
                {
                    _context.Set<Attendence>().RemoveRange(relatedAttendance);
                }

                _context.Set<Operators>().Remove(operario);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> BaseReport()
        {
            var operators = await _context.Operators
                .Include(o => o.Area)
                .Include(o => o.Linea)
                .OrderBy(o => o.Name)
                .ToListAsync();
            var lines = operators
                .Where(o => o.Linea != null)
                .Select(o => o.Linea!)
                .Distinct()
                .OrderBy(l => l.Name)
                .ToList();

            var model = new StjacksAssistens.ViewModels.BaseReportViewModel
            {
                Operators = operators,
                Lines = lines
            };

            return View(model);
        }
    }

}