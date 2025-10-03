using Microsoft.AspNetCore.Mvc;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System.Linq;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class VolunteerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VolunteerController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var volunteers = _context.Volunteers.ToList();
            return View(volunteers);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Volunteer volunteer)
        {
            if (ModelState.IsValid)
            {
                _context.Volunteers.Add(volunteer);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(volunteer);
        }

        public IActionResult Details(int id)
        {
            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null) return NotFound();
            return View(volunteer);
        }

        public IActionResult Edit(int id)
        {
            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null) return NotFound();
            return View(volunteer);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Volunteer volunteer)
        {
            if (id != volunteer.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Volunteers.Update(volunteer);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(volunteer);
        }

        public IActionResult Delete(int id)
        {
            var volunteer = _context.Volunteers.Find(id);
            if (volunteer == null) return NotFound();
            return View(volunteer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var volunteer = _context.Volunteers.Find(id);
            if (volunteer != null)
            {
                _context.Volunteers.Remove(volunteer);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
