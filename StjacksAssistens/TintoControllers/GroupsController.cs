using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace StjacksAssistens.TintoControllers
{
    public class GroupsController : Controller
    {
        // GET: GroupsController
        public ActionResult Index()
        {
            return View();
        }

        // GET: GroupsController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: GroupsController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: GroupsController/Create
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

        // GET: GroupsController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: GroupsController/Edit/5
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

        // GET: GroupsController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: GroupsController/Delete/5
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
