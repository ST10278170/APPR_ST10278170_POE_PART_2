using Microsoft.AspNetCore.Mvc;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using Microsoft.EntityFrameworkCore;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class TaskAssignmentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TaskAssignmentController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var tasks = await _context.TaskAssignments.ToListAsync();
            return View(tasks);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskAssignment task)
        {
            if (ModelState.IsValid)
            {
                _context.TaskAssignments.Add(task);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }

        public async Task<IActionResult> Details(int id)
        {
            var task = await _context.TaskAssignments.FindAsync(id);
            if (task == null) return NotFound();
            return View(task);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.TaskAssignments.FindAsync(id);
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TaskAssignment task)
        {
            if (id != task.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Update(task);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(task);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.TaskAssignments.FindAsync(id);
            if (task == null) return NotFound();
            return View(task);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.TaskAssignments.FindAsync(id);
            if (task != null)
            {
                _context.TaskAssignments.Remove(task);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
