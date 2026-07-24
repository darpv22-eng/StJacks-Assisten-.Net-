using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.TintoData;
using StjacksAssistens.TintoModels;

namespace StjacksAssistens.TintoControllers
{
    public class OperatorsTintosController : Controller
    {
        private readonly TintoDbContext _context;

        public OperatorsTintosController(TintoDbContext context)
        {
            _context = context;
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Details(int id)
        {
            return View();
        }

        // Único método POST Create para guardar el operario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OperatorsTintos operatorsTintos)
        {
            operatorsTintos.OperatorsTintosId = 0; // Evita mandar un ID en cero a la BD

            if (ModelState.IsValid)
            {
                _context.Add(operatorsTintos);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Groups");
            }

            return RedirectToAction("Index", "Groups");
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