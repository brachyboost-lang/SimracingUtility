using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SimracingUtility.Controllers
{
    public class SetupController : Controller
    {
        // GET: SetupController
        public ActionResult Index()
        {
            return View();
        }

        // GET: SetupController/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: SetupController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: SetupController/Create
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

        // GET: SetupController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: SetupController/Edit/5
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

        // GET: SetupController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: SetupController/Delete/5
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
