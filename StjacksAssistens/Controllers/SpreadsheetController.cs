using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.Data;
using System.ComponentModel.Design;

namespace StjacksAssistens.Controllers
{
    public class SpreadsheetController : Controller
    {

        //Menu donde estan todas las planilas, para elegir cual se quiere ver, y luego redirige a cada una de las planillas
        private readonly ApplicationDbContext _context;
        public SpreadsheetController(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index()
        {
            var ultimoPeriodo = await _context.Periodss
                .OrderByDescending(p => p.Id)
                .FirstOrDefaultAsync();
            var model = new
            {
                PeriodoActualId = ultimoPeriodo?.Id ?? 0
            };

            return View(model);
        }
        public ActionResult Details(int id)
        {
            return View();
        }
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