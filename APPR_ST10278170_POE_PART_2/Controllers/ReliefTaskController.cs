using Microsoft.AspNetCore.Mvc;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System.Linq;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class ReliefTaskController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReliefTaskController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var tasks = _context.ReliefTasks.ToList();
            return View(tasks);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(ReliefTask task)
        {
            if (ModelState.IsValid)
            {
                _context.ReliefTasks.Add(task);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }

        public IActionResult Details(int id)
        {
            var task = _context.ReliefTasks.Find(id);
            if (task == null) return NotFound();
            return View(task);
        }

        public IActionResult Edit(int id)
        {
            var task = _context.ReliefTasks.Find(id);
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, ReliefTask task)
        {
            if (id != task.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.ReliefTasks.Update(task);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }

        public IActionResult Delete(int id)
        {
            var task = _context.ReliefTasks.Find(id);
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var task = _context.ReliefTasks.Find(id);
            if (task != null)
            {
                _context.ReliefTasks.Remove(task);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
