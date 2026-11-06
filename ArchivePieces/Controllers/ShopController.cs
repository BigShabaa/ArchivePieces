using ArchivePieces.Data;
using ArchivePieces.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePieces.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _db;
        private const int PageSize = 12;

        public ShopController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(
            string? category, string? size, decimal? minPrice, decimal? maxPrice,
            string sortBy = "newest", int page = 1)
        {
            var query = _db.Products
                .Where(p => p.IsActive)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category.Slug == category);

            if (!string.IsNullOrEmpty(size))
                query = query.Where(p => p.Variants.Any(v => v.Size == size));

            if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

            query = sortBy switch
            {
                "price-asc"  => query.OrderBy(p => p.Price),
                "price-desc" => query.OrderByDescending(p => p.Price),
                _            => query.OrderByDescending(p => p.CreatedAt),
            };

            var total = await query.CountAsync();

            var products = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            var vm = new ProductListViewModel
            {
                Products = products,
                Categories = await _db.Categories
                    .Where(c => c.IsVisible)
                    .OrderBy(c => c.DisplayOrder)
                    .ToListAsync(),
                SelectedCategory = category,
                SelectedSize = size,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                Page = page,
                TotalCount = total,
                TotalPages = (int)Math.Ceiling(total / (double)PageSize)
            };

            return View(vm);
        }
    }

    public class ProductController : Controller
    {
        private readonly AppDbContext _db;

        public ProductController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Detail(string slug)
        {
            var product = await _db.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

            if (product == null) return NotFound();

            var related = await _db.Products
                .Where(p => p.CategoryId == product.CategoryId
                         && p.Id != product.Id
                         && p.IsActive)
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .Take(4)
                .ToListAsync();

            return View(new ViewModels.ProductDetailViewModel
            {
                Product = product,
                RelatedProducts = related
            });
        }
    }
}
