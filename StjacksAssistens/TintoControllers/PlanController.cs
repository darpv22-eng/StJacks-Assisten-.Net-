using ExcelDataReader;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StjacksAssistens.TintoData;
using StjacksAssistens.TintoModels;
using System.Data;
using System.IO;
using System.Text;

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

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var tempDirectory = Path.GetTempPath();
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(archivoExcel.FileName);
            var filePath = Path.Combine(tempDirectory, fileName);

            try
            {
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await archivoExcel.CopyToAsync(stream);
                }

                using (var stream = System.IO.File.Open(filePath, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = ExcelDataReader.ExcelReaderFactory.CreateReader(stream))
                    {
                        var result = reader.AsDataSet(new ExcelDataReader.ExcelDataSetConfiguration()
                        {
                            ConfigureDataTable = (_) => new ExcelDataReader.ExcelDataTableConfiguration()
                            {
                                UseHeaderRow = false
                            }
                        });

                        var table = result.Tables[0];
                        DateTime? ultimaFechaIzq = null;
                        DateTime? ultimaFechaDer = null;

                        for (int i = 1; i < table.Rows.Count; i++)
                        {
                            var row = table.Rows[i];

                            // ==========================================
                            // BLOQUE IZQUIERDO (Columnas A a G)
                            // ==========================================
                            string loteIzq = row[1]?.ToString()?.Trim() ?? string.Empty;

                            if (!string.IsNullOrEmpty(loteIzq) && !loteIzq.Contains("Suma") && !loteIzq.Contains("Total") && !loteIzq.Equals("LOTES", StringComparison.OrdinalIgnoreCase))
                            {
                                if (DateTime.TryParse(row[0]?.ToString(), out DateTime fechaIzq))
                                {
                                    ultimaFechaIzq = fechaIzq;
                                }

                                var planIzq = new PlanDelivery
                                {
                                    DeliveryDate = ultimaFechaIzq ?? DateTime.Now,
                                    LoteCode = loteIzq,
                                    PrintColoJumb = row[2]?.ToString()?.Trim() ?? string.Empty,
                                    SumKl = decimal.TryParse(row[3]?.ToString(), out decimal kIzq) ? kIzq : 0,
                                    SumRolls = 0,
                                    Status = row[4]?.ToString()?.Trim() ?? "Entregado",
                                    Comments = row.ItemArray.Length > 6 ? (row[6]?.ToString()?.Trim() ?? string.Empty) : string.Empty
                                };

                                _context.Set<PlanDelivery>().Add(planIzq);
                            }

                            // ==========================================
                            // BLOQUE DERECHO (Columnas I a M)
                            // ==========================================
                            if (row.ItemArray.Length > 9)
                            {
                                string loteDer = row[9]?.ToString()?.Trim() ?? string.Empty;

                                if (!string.IsNullOrEmpty(loteDer) && !loteDer.Contains("Suma") && !loteDer.Contains("Total") && !loteDer.Equals("LOTES", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (DateTime.TryParse(row[8]?.ToString(), out DateTime fechaDer))
                                    {
                                        ultimaFechaDer = fechaDer;
                                    }

                                    var planDer = new PlanDelivery
                                    {
                                        DeliveryDate = ultimaFechaDer ?? DateTime.Now,
                                        LoteCode = loteDer,
                                        PrintColoJumb = row[10]?.ToString()?.Trim() ?? string.Empty,
                                        SumKl = decimal.TryParse(row[11]?.ToString(), out decimal kDer) ? kDer : 0,
                                        SumRolls = 0,
                                        Status = row[12]?.ToString()?.Trim() ?? "Entregado",
                                        Comments = string.Empty
                                    };

                                    _context.Set<PlanDelivery>().Add(planDer);
                                }
                            }
                        }

                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Mensaje"] = "¡El plan de entregas doble se ha importado correctamente!";
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                TempData["Error"] = $"Ocurrió un error al procesar el archivo: {innerMessage}";
            }
            finally
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
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