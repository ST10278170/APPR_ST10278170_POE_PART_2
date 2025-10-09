using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System.Threading.Tasks;
using System.Linq;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class DisasterReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DisasterReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 🔍 GET: /DisasterReport
        public async Task<IActionResult> Index()
        {
            var reports = await _context.DisasterReports.ToListAsync();
            return View(reports);
        }

        // 📝 GET: /DisasterReport/Create
        public IActionResult Create()
        {
            return View();
        }

        // ✅ POST: /DisasterReport/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,DisasterType,Location,DateReported,Description,ReporterName,IsVerified")] DisasterReport report)
        {
            if (!ModelState.IsValid)
            {
                return View(report);
            }

            try
            {
                _context.DisasterReports.Add(report);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                ModelState.AddModelError("", "An error occurred while creating the disaster report.");
                return View(report);
            }
        }

        // 🔍 GET: /DisasterReport/Details/{id}
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var report = await _context.DisasterReports.FirstOrDefaultAsync(m => m.Id == id);
            if (report == null)
                return NotFound();

            return View(report);
        }

        // ✏️ GET: /DisasterReport/Edit/{id}
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var report = await _context.DisasterReports.FindAsync(id);
            if (report == null)
                return NotFound();

            return View(report);
        }

        // ✅ POST: /DisasterReport/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,DisasterType,Location,DateReported,Description,ReporterName,IsVerified")] DisasterReport report)
        {
            if (id != report.Id)
                return NotFound();

            if (!ModelState.IsValid)
                return View(report);

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

        // 🗑️ GET: /DisasterReport/Delete/{id}
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var report = await _context.DisasterReports
                .FirstOrDefaultAsync(m => m.Id == id);
            if (report == null)
                return NotFound();

            return View(report);
        }

        // ✅ POST: /DisasterReport/DeleteConfirmed/{id}
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
