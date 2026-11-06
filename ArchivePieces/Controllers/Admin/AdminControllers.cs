using ArchivePieces.Data;
using ArchivePieces.Models;
using ArchivePieces.Services;
using ArchivePieces.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePieces.Controllers.Admin
{
    [Authorize(Roles = "Admin")]
    [Route("admin")]
    public class AdminDashboardController : Controller
    {
        private readonly AppDbContext _db;

        public AdminDashboardController(AppDbContext db) => _db = db;

        [Route(""), Route("dashboard")]
        public async Task<IActionResult> Index()
        {
            var vm = new DashboardViewModel
            {
                TotalProducts = await _db.Products.CountAsync(p => p.IsActive),
                TotalOrders = await _db.Orders.CountAsync(),
                PendingOrders = await _db.Orders.CountAsync(o => o.Status == OrderStatus.Pending),
                OutOfStockVariants = await _db.ProductVariants.CountAsync(v => v.StockQuantity == 0),
                RecentOrders = await _db.Orders
                    .Include(o => o.Items)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(10)
                    .ToListAsync()
            };
            return View("~/Views/Admin/Dashboard/Index.cshtml", vm);
        }
    }

    // ── PRODUCTS ──────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    [Route("admin/products")]
    public class AdminProductController : Controller
    {
        private readonly AppDbContext _db;
        private readonly ImageUploadService _images;

        public AdminProductController(AppDbContext db, ImageUploadService images)
        {
            _db = db;
            _images = images;
        }

        [Route("")]
        public async Task<IActionResult> Index(string? search, int page = 1)
        {
            const int ps = 20;
            var q = _db.Products
                .Include(p => p.Category)
                .Include(p => p.Variants)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                q = q.Where(p => p.Name.Contains(search));

            q = q.OrderByDescending(p => p.CreatedAt);

            var total = await q.CountAsync();
            var products = await q.Skip((page - 1) * ps).Take(ps).ToListAsync();

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.TotalPages = (int)Math.Ceiling(total / (double)ps);

            return View("~/Views/Admin/Products/Index.cshtml", products);
        }

        [Route("create")]
        public async Task<IActionResult> Create()
        {
            return View("~/Views/Admin/Products/Form.cshtml", new ProductFormViewModel
            {
                Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync(),
                Variants = new List<VariantFormItem>
                {
                    new() { Size = "XS" }, new() { Size = "S" }, new() { Size = "M" },
                    new() { Size = "L"  }, new() { Size = "XL" }, new() { Size = "XXL" }
                }
            });
        }

        [HttpPost, Route("create"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductFormViewModel vm)
        {
            await LoadCategories(vm);
            if (!ModelState.IsValid) return View("~/Views/Admin/Products/Form.cshtml", vm);

            var product = new Product
            {
                Name = vm.Name.ToUpperInvariant(),
                Slug = string.IsNullOrWhiteSpace(vm.Slug)
                    ? GenerateSlug(vm.Name) : vm.Slug.ToLowerInvariant(),
                Description = vm.Description,
                Price = vm.Price,
                CategoryId = vm.CategoryId,
                IsActive = vm.IsActive,
                IsFeatured = vm.IsFeatured,
                CreatedAt = DateTime.UtcNow
            };

            // Variants
            foreach (var v in vm.Variants.Where(v => !v.Delete && !string.IsNullOrEmpty(v.Size)))
                product.Variants.Add(new ProductVariant { Size = v.Size, StockQuantity = v.StockQuantity });

            _db.Products.Add(product);
            await _db.SaveChangesAsync();

            // Images (after save so we have the Id)
            await UploadImages(vm.NewImages, product);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Product created.";
            return RedirectToAction("Index");
        }

        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var p = await _db.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Variants)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (p == null) return NotFound();

            var vm = new ProductFormViewModel
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                Description = p.Description,
                Price = p.Price,
                CategoryId = p.CategoryId,
                IsActive = p.IsActive,
                IsFeatured = p.IsFeatured,
                ExistingImages = p.Images.ToList(),
                Variants = p.Variants.Select(v => new VariantFormItem
                {
                    Id = v.Id, Size = v.Size, StockQuantity = v.StockQuantity
                }).ToList(),
                Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync()
            };

            return View("~/Views/Admin/Products/Form.cshtml", vm);
        }

        [HttpPost, Route("edit/{id}"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductFormViewModel vm)
        {
            await LoadCategories(vm);
            if (!ModelState.IsValid)
            {
                vm.ExistingImages = await _db.ProductImages
                    .Where(i => i.ProductId == id).ToListAsync();
                return View("~/Views/Admin/Products/Form.cshtml", vm);
            }

            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            product.Name = vm.Name.ToUpperInvariant();
            product.Slug = string.IsNullOrWhiteSpace(vm.Slug)
                ? GenerateSlug(vm.Name) : vm.Slug.ToLowerInvariant();
            product.Description = vm.Description;
            product.Price = vm.Price;
            product.CategoryId = vm.CategoryId;
            product.IsActive = vm.IsActive;
            product.IsFeatured = vm.IsFeatured;

            // Delete selected images
            foreach (var imgId in vm.DeleteImageIds)
            {
                var img = product.Images.FirstOrDefault(i => i.Id == imgId);
                if (img != null)
                {
                    _images.Delete(img.ImagePath);
                    _db.ProductImages.Remove(img);
                }
            }

            // Handle variants
            foreach (var vf in vm.Variants)
            {
                if (vf.Id > 0)
                {
                    var existing = product.Variants.FirstOrDefault(v => v.Id == vf.Id);
                    if (existing != null)
                    {
                        if (vf.Delete)
                            _db.ProductVariants.Remove(existing);
                        else
                            existing.StockQuantity = vf.StockQuantity;
                    }
                }
                else if (!vf.Delete && !string.IsNullOrEmpty(vf.Size))
                {
                    product.Variants.Add(new ProductVariant
                    {
                        Size = vf.Size,
                        StockQuantity = vf.StockQuantity
                    });
                }
            }

            await UploadImages(vm.NewImages, product);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Product updated.";
            return RedirectToAction("Index");
        }

        [HttpPost, Route("delete/{id}"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var p = await _db.Products.FindAsync(id);
            if (p != null) { p.IsActive = false; await _db.SaveChangesAsync(); }
            TempData["Success"] = "Product deactivated.";
            return RedirectToAction("Index");
        }

        // helpers
        private async Task LoadCategories(ProductFormViewModel vm)
        {
            vm.Categories = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
        }

        private async Task UploadImages(List<IFormFile> files, Product product)
        {
            var order = product.Images.Count;
            foreach (var file in files)
            {
                var (ok, path, _) = await _images.UploadAsync(file, product.Id);
                if (ok && path != null)
                    product.Images.Add(new ProductImage { ImagePath = path, DisplayOrder = order++ });
            }
        }

        private static string GenerateSlug(string name)
        {
            return System.Text.RegularExpressions.Regex
                .Replace(name.ToLowerInvariant(), @"[^a-z0-9]+", "-")
                .Trim('-');
        }
    }

    // ── CATEGORIES ────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    [Route("admin/categories")]
    public class AdminCategoryController : Controller
    {
        private readonly AppDbContext _db;
        public AdminCategoryController(AppDbContext db) => _db = db;

        [Route("")]
        public async Task<IActionResult> Index()
        {
            var cats = await _db.Categories.OrderBy(c => c.DisplayOrder).ToListAsync();
            return View("~/Views/Admin/Categories/Index.cshtml", cats);
        }

        [Route("create")]
        public IActionResult Create() =>
            View("~/Views/Admin/Categories/Form.cshtml", new CategoryFormViewModel());

        [HttpPost, Route("create"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryFormViewModel vm)
        {
            if (!ModelState.IsValid) return View("~/Views/Admin/Categories/Form.cshtml", vm);
            _db.Categories.Add(new Category
            {
                Name = vm.Name.ToUpperInvariant(),
                Slug = string.IsNullOrWhiteSpace(vm.Slug)
                    ? vm.Name.ToLowerInvariant().Replace(" ", "-") : vm.Slug,
                DisplayOrder = vm.DisplayOrder,
                IsVisible = vm.IsVisible
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category created.";
            return RedirectToAction("Index");
        }

        [Route("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Categories.FindAsync(id);
            if (c == null) return NotFound();
            return View("~/Views/Admin/Categories/Form.cshtml", new CategoryFormViewModel
            {
                Id = c.Id, Name = c.Name, Slug = c.Slug,
                DisplayOrder = c.DisplayOrder, IsVisible = c.IsVisible
            });
        }

        [HttpPost, Route("edit/{id}"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CategoryFormViewModel vm)
        {
            if (!ModelState.IsValid) return View("~/Views/Admin/Categories/Form.cshtml", vm);
            var c = await _db.Categories.FindAsync(id);
            if (c == null) return NotFound();
            c.Name = vm.Name.ToUpperInvariant();
            c.Slug = vm.Slug;
            c.DisplayOrder = vm.DisplayOrder;
            c.IsVisible = vm.IsVisible;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Category updated.";
            return RedirectToAction("Index");
        }

        [HttpPost, Route("delete/{id}"), ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Categories.FindAsync(id);
            if (c != null) { _db.Categories.Remove(c); await _db.SaveChangesAsync(); }
            TempData["Success"] = "Category deleted.";
            return RedirectToAction("Index");
        }
    }

    // ── ORDERS ────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    [Route("admin/orders")]
    public class AdminOrderController : Controller
    {
        private readonly AppDbContext _db;
        public AdminOrderController(AppDbContext db) => _db = db;

        [Route("")]
        public async Task<IActionResult> Index(
            string? status, string? search, DateTime? from, DateTime? to, int page = 1)
        {
            const int ps = 20;
            var q = _db.Orders.Include(o => o.Items).AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, out var s))
                q = q.Where(o => o.Status == s);
            if (!string.IsNullOrEmpty(search))
                q = q.Where(o => o.OrderNumber.Contains(search)
                              || o.CustomerName.Contains(search)
                              || o.CustomerEmail.Contains(search));
            if (from.HasValue) q = q.Where(o => o.CreatedAt >= from.Value);
            if (to.HasValue)   q = q.Where(o => o.CreatedAt <= to.Value.AddDays(1));

            q = q.OrderByDescending(o => o.CreatedAt);

            var total = await q.CountAsync();
            var orders = await q.Skip((page - 1) * ps).Take(ps).ToListAsync();

            return View("~/Views/Admin/Orders/Index.cshtml", new OrderListViewModel
            {
                Orders = orders,
                StatusFilter = status,
                Search = search,
                From = from,
                To = to,
                Page = page,
                TotalPages = (int)Math.Ceiling(total / (double)ps)
            });
        }

        [Route("{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items).ThenInclude(i => i.Product)
                .Include(o => o.StatusHistory.OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View("~/Views/Admin/Orders/Detail.cshtml", new OrderDetailViewModel
            {
                Order = order,
                AvailableStatuses = Enum.GetValues<OrderStatus>().ToList()
            });
        }

        [HttpPost, Route("{id}/status"), ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
        {
            var order = await _db.Orders
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = status,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = User.Identity?.Name ?? "Admin"
            });

            await _db.SaveChangesAsync();
            TempData["Success"] = $"Order status updated to {status}.";
            return RedirectToAction("Detail", new { id });
        }
    }

    // ── STOCK ─────────────────────────────────────────────────────────────────
    [Authorize(Roles = "Admin")]
    [Route("admin/stock")]
    public class AdminStockController : Controller
    {
        private readonly AppDbContext _db;
        public AdminStockController(AppDbContext db) => _db = db;

        [Route("")]
        public async Task<IActionResult> Index(string? search)
        {
            var q = _db.ProductVariants
                .Include(v => v.Product)
                .Where(v => v.Product.IsActive)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
                q = q.Where(v => v.Product.Name.Contains(search));

            var rows = await q
                .OrderBy(v => v.Product.Name).ThenBy(v => v.Size)
                .Select(v => new StockRow
                {
                    VariantId = v.Id,
                    ProductId = v.ProductId,
                    ProductName = v.Product.Name,
                    Size = v.Size,
                    StockQuantity = v.StockQuantity
                })
                .ToListAsync();

            return View("~/Views/Admin/Stock/Index.cshtml", new StockViewModel
            {
                Rows = rows, Search = search
            });
        }

        [HttpPost, Route("update"), ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStock(int variantId, int stock)
        {
            var v = await _db.ProductVariants.FindAsync(variantId);
            if (v != null) { v.StockQuantity = Math.Max(0, stock); await _db.SaveChangesAsync(); }
            return Ok();
        }
    }
}
