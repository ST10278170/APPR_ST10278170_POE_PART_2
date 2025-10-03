using Microsoft.AspNetCore.Mvc;
using APPR_ST10278170_POE_PART_2.Data;
using APPR_ST10278170_POE_PART_2.Models;
using System.Linq;

namespace APPR_ST10278170_POE_PART_2.Controllers
{
    public class DonationController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DonationController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var donations = _context.Donations.ToList();
            return View(donations);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Donation donation)
        {
            if (ModelState.IsValid)
            {
                _context.Donations.Add(donation);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(donation);
        }

        public IActionResult Details(int id)
        {
            var donation = _context.Donations.FirstOrDefault(d => d.Id == id);
            if (donation == null) return NotFound();
            return View(donation);
        }

        public IActionResult Edit(int id)
        {
            var donation = _context.Donations.FirstOrDefault(d => d.Id == id);
            if (donation == null) return NotFound();
            return View(donation);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Donation donation)
        {
            if (id != donation.Id) return NotFound();
            if (ModelState.IsValid)
            {
                _context.Donations.Update(donation);
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(donation);
        }

        public IActionResult Delete(int id)
        {
            var donation = _context.Donations.FirstOrDefault(d => d.Id == id);
            if (donation == null) return NotFound();
            return View(donation);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var donation = _context.Donations.FirstOrDefault(d => d.Id == id);
            if (donation != null)
            {
                _context.Donations.Remove(donation);
                _context.SaveChanges();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
