using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using StjacksAssistens.TintoData;
using StjacksAssistens.TintoModels;

namespace StjacksAssistens.TintoControllers
{
    public class PlanTintoController : Controller
    {
        private readonly TintoDbContext _context;

        // Constructor para inyectar el contexto de la base de datos
        public PlanTintoController(TintoDbContext context)
        {
            _context = context;
        }

        // GET: PlanTinto
        public async Task<IActionResult> Index()
        {
            var planes = await _context.Set<PlanDelivery>().ToListAsync();
            return View("~/Views/PlanTinto/Index.cshtml", planes);
        }

        // POST: PlanTinto/SubirExcel
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubirExcel(IFormFile archivoExcel)
        {
            if (archivoExcel == null || archivoExcel.Length == 0)
            {
                TempData["Error"] = "Por favor selecciona un archivo Excel válido.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                using (var stream = new MemoryStream())
                {
                    await archivoExcel.CopyToAsync(stream);
                    using (var workbook = new XLWorkbook(stream))
                    {
                        var worksheet = workbook.Worksheet(1);
                        var rows = worksheet.RowsUsed().Skip(1); // Salta la cabecera

                        foreach (var row in rows)
                        {
                            var plan = new PlanDelivery
                            {
                                LoteCode = row.Cell(1).GetValue<string>(),
                                DeliveryDate = row.Cell(2).TryGetValue(out DateTime fecha) ? fecha : (DateTime?)null,
                                PrintColoJumb = row.Cell(3).GetValue<string>(),
                                SumKl = row.Cell(4).TryGetValue(out decimal kilos) ? kilos : 0,
                                SumRolls = row.Cell(5).TryGetValue(out int rollos) ? rollos : 0,
                                Status = row.Cell(6).GetValue<string>(),
                                Comments = row.Cell(7).GetValue<string>() ?? string.Empty
                            };

                            _context.Set<PlanDelivery>().Add(plan);
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Mensaje"] = "¡El plan de entregas se ha importado y guardado correctamente!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ocurrió un error al procesar el archivo: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: PlanTinto/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: PlanTinto/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: PlanTinto/Create
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

        // GET: PlanTinto/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: PlanTinto/Edit/5
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

        // GET: PlanTinto/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: PlanTinto/Delete/5
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