using Microsoft.AspNetCore.Http;
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
        // --- LISTAR CON FILTRO ---
        public async Task<IActionResult> Index(int? categoryId)
        {
            var categories = await _context.Category.ToListAsync();
            ViewBag.Categories = categories;
            ViewBag.SelectedCategory = categoryId;

            var query = _context.Operators.Include(o => o.Category).AsQueryable();

            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(o => o.CategoryId == categoryId);
            }

            return View(await query.ToListAsync());
        }
        // --- AGREGAR (Vista) ---
        public async Task<IActionResult> Create()
        {
            var categories = await _context.Category.ToListAsync();
            ViewBag.CategoryList = new SelectList(categories, "Id", "Name");
            return View();
        }

        // --- AGREGAR (Guardar) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Operators operatorModel)
        {
            if (ModelState.IsValid)
            {
                _context.Add(operatorModel);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(operatorModel);
        }

        // --- EDITAR (Vista) ---
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var operatorModel = await _context.Operators.FindAsync(id);
            if (operatorModel == null) return NotFound();

            return View(operatorModel);
        }

        // --- EDITAR (Guardar) ---
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

        // --- ELIMINAR (Vista de confirmación) ---
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var operatorModel = await _context.Operators
                .FirstOrDefaultAsync(m => m.Id == id);

            if (operatorModel == null) return NotFound();

            return View(operatorModel);
        }

        // --- ELIMINAR (Confirmar) ---
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
