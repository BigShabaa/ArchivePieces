using ArchivePieces.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePieces.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;

        public HomeController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var featured = await _db.Products
                .Where(p => p.IsActive && p.IsFeatured)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Fallback: if no featured, show newest
            if (!featured.Any())
            {
                featured = await _db.Products
                    .Where(p => p.IsActive)
                    .Include(p => p.Images)
                    .Include(p => p.Variants)
                    .Include(p => p.Category)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync();
            }

            var categories = await _db.Categories
                .Where(c => c.IsVisible)
                .OrderBy(c => c.DisplayOrder)
                .ToListAsync();

            ViewBag.Categories = categories;
            return View(featured);
        }
    }
}
