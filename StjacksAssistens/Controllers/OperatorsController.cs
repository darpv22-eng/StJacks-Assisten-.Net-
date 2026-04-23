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
            // 1. Obtener periodo (seleccionado, el último o null)
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

            // 2. Generar días laborales (Lunes a Viernes)
            var days = new List<DateTime>();
            for (var dt = period.StartDate; dt <= period.EndDate; dt = dt.AddDays(1))
            {
                if (dt.DayOfWeek != DayOfWeek.Saturday && dt.DayOfWeek != DayOfWeek.Sunday)
                    days.Add(dt);
            }

            // 3. Consulta de Operarios
            var query = _context.Operators.Include(o => o.Category).AsQueryable();
            if (categoryId.HasValue) query = query.Where(o => o.CategoryId == categoryId);
            var operators = await query.ToListAsync();

            // 4. Asistencia del periodo
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
                        d => allAttendance.FirstOrDefault(a => a.OperatorsId == o.Id && a.AttendanceDate.Date == d.Date)?.Status ?? "X"
                    )
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateAttendance(int operatorId, string date, string status, int periodId)
        {
            var attendanceDate = DateTime.Parse(date);
            var attendance = await _context.Attendence
                .FirstOrDefaultAsync(a => a.OperatorsId == operatorId && a.AttendanceDate.Date == attendanceDate.Date && a.PeriodId == periodId);

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
            try
            {
                // Forzamos que el ModelState sea válido o simplemente intentamos guardar
                _context.Add(op);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // Si hay error, puedes usar un TempData para avisar al usuario
                TempData["Error"] = "Error al crear: Verifique que la categoría sea válida.";
                return RedirectToAction(nameof(Index));
            }
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var operatorModel = await _context.Operators.FindAsync(id);
            if (operatorModel == null) return NotFound();
            return View(operatorModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Operators operatorModel)
        {
            if (id != operatorModel.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(operatorModel);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OperatorExists(operatorModel.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(operatorModel);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var operatorModel = await _context.Operators
                .Include(o => o.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (operatorModel == null) return NotFound();
            return View(operatorModel);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var operatorModel = await _context.Operators.FindAsync(id);
            if (operatorModel != null)
            {
                _context.Operators.Remove(operatorModel);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool OperatorExists(int id)
        {
            return _context.Operators.Any(e => e.Id == id);
        }
    }
}