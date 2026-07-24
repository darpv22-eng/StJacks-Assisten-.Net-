using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.TintoData;
using StjacksAssistens.TintoModels;

namespace StjacksAssistens.TintoControllers
{
    public class GroupsController : Controller
    {
        private readonly TintoDbContext _context;

        public GroupsController(TintoDbContext context)
        {
            _context = context;
        }

        // Carga la lista de grupos y la envía a la vista ubicada en la carpeta TintoGroups
        public async Task<IActionResult> Index()
        {
            var viewModel = new TintoDashboardViewModel
            {
                Groups = await _context.Groups.ToListAsync(),
                Operators = await _context.OperatorsTintos.ToListAsync()
            };

            return View("~/Views/TintoGroups/Index.cshtml", viewModel);
        }

        // Método POST para crear operarios
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create(OperatorsTinto operatorsTinto)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Add(operatorsTinto);
        //        await _context.SaveChangesAsync();
        //    }

        //    return RedirectToAction("Index", "Index");
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Groups group)
        {
            if (ModelState.IsValid)
            {
                _context.Add(group);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Método POST para editar operarios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, OperatorsTintos operatorsTintos)
        {
            if (id != operatorsTintos.OperatorsTintosId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(operatorsTintos);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.OperatorsTintos.Any(e => e.OperatorsTintosId == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", "Groups");
            }

            return RedirectToAction("Index", "Groups");
        }

        // Método POST para eliminar operarios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var operatorsTinto = await _context.OperatorsTintos.FindAsync(id);
            if (operatorsTinto != null)
            {
                _context.OperatorsTintos.Remove(operatorsTinto);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Index", "Groups");
        }
    }
}