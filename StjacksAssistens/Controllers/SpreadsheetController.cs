using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Data;

namespace StjacksAssistens.Controllers
{
    public class SpreadsheetController : Controller
    {
        private readonly ApplicationDbContext _context;

        // El constructor inyecta la base de datos
        public SpreadsheetController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Spreadsheet
        public async Task<IActionResult> Index()
        {
            // Buscamos el periodo más reciente en la base de datos
            var ultimoPeriodo = await _context.Periodss
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();

            // Creamos un objeto anónimo para pasar el ID a la vista
            var model = new
            {
                PeriodoActualId = ultimoPeriodo?.Id ?? 0
            };

            return View(model);
        }

        // GET: SpreadsheetController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SpreadsheetController/Create
        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SpreadsheetController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: SpreadsheetController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}