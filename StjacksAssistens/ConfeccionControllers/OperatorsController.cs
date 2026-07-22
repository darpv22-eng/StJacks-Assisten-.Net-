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
            // 1. Busca el registro usando el nombre correcto de la propiedad: OperatorsId
            var attendance = await _context.Attendence
                .FirstOrDefaultAsync(a => a.OperatorsId == operatorId && a.AttendanceDate.Date == date.Date && a.PeriodId == periodId);

            if (attendance == null)
            {
                // Si no existe, créalo
                attendance = new Attendence
                {
                    OperatorsId = operatorId, // También aquí
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

            // TryParse evita un 500 si llega una hora mal formada desde el cliente
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

        // Antes era GET: borrar datos con un simple enlace era peligroso (CSRF, prefetch del navegador)
        [HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
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

        //// Elimina un operario y su asistencia asociada. POST + antiforgery + solo Admin
        //// (la vista de Asistencia envía el token en un form dinámico).
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //[Authorize(Roles = "Admin")]
        //public async Task<IActionResult> DeleteConfirmed(int id)
        //{
        //    var operario = await _context.Set<Operators>().FindAsync(id);
        //    if (operario != null)
        //    {
        //        var relatedAttendance = _context.Set<Attendence>().Where(a => a.OperatorsId == id);
        //        _context.Set<Attendence>().RemoveRange(relatedAttendance);
        //        _context.Set<Operators>().Remove(operario);
        //        await _context.SaveChangesAsync();
        //    }
        //    return RedirectToAction(nameof(Index));
        //}
    }
}