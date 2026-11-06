using ArchivePieces.Data;
using ArchivePieces.Models;
using ArchivePieces.Services;
using ArchivePieces.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePieces.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cart;
        private readonly AppDbContext _db;

        public CartController(CartService cart, AppDbContext db)
        {
            _cart = cart;
            _db = db;
        }

        public IActionResult Index()
        {
            return View(new CartViewModel { Cart = _cart.GetCart() });
        }

        [HttpPost]
        public async Task<IActionResult> Add(int productId, string size, int quantity = 1)
        {
            var product = await _db.Products
                .Include(p => p.Images)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product == null) return NotFound();

            var variant = product.Variants.FirstOrDefault(v => v.Size == size);
            if (variant == null || variant.StockQuantity < quantity)
            {
                TempData["Error"] = "Selected size is unavailable.";
                return RedirectToAction("Detail", "Product", new { slug = product.Slug });
            }

            _cart.AddItem(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSlug = product.Slug,
                Size = size,
                UnitPrice = product.Price,
                Quantity = quantity,
                ImagePath = product.PrimaryImagePath
            });

            TempData["Success"] = $"{product.Name} added to cart.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Update(int productId, string size, int quantity)
        {
            _cart.UpdateQuantity(productId, size, quantity);
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult Remove(int productId, string size)
        {
            _cart.RemoveItem(productId, size);
            return RedirectToAction("Index");
        }

        // AJAX endpoint for cart count in navbar
        public IActionResult Count()
        {
            return Json(new { count = _cart.GetItemCount() });
        }
    }
}
