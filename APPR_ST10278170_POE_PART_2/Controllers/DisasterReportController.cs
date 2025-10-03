using Microsoft.AspNetCore.Mvc;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using Microsoft.EntityFrameworkCore;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class DisasterReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DisasterReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔍 List all reports
        public async Task<IActionResult> Index()
        {
            var reports = await _context.DisasterReports.ToListAsync();
            return View(reports);
        }

        // 📝 Show create form
        public IActionResult Create()
        {
            return View();
        }

        // ✅ Handle form submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DisasterReport report)
        {
            if (ModelState.IsValid)
            {
                _context.Add(report);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(report);
        }

        // 🔍 View details
        public async Task<IActionResult> Details(int id)
        {
            var report = await _context.DisasterReports.FindAsync(id);
            if (report == null) return NotFound();
            return View(report);
        }

        // ✏️ Show edit form
        public async Task<IActionResult> Edit(int id)
        {
            var report = await _context.DisasterReports.FindAsync(id);
            if (report == null) return NotFound();
            return View(report);
        }

        // ✅ Handle edit submission
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, DisasterReport report)
        {
            if (id != report.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(report);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.DisasterReports.Any(e => e.Id == id))
                        return NotFound();
                    throw;
                }
            }
            return View(report);
        }

        // 🗑️ Show delete confirmation
        public async Task<IActionResult> Delete(int id)
        {
            var report = await _context.DisasterReports.FindAsync(id);
            if (report == null) return NotFound();
            return View(report);
        }

        // ✅ Handle delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var report = await _context.DisasterReports.FindAsync(id);
            if (report != null)
            {
                _context.DisasterReports.Remove(report);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
