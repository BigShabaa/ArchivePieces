using ArchivePieces.Data;
using ArchivePieces.Models;
using ArchivePieces.Services;
using ArchivePieces.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ArchivePieces.Controllers
{
    public class CheckoutController : Controller
    {
        private readonly CartService _cart;
        private readonly AppDbContext _db;
        private readonly OrderNumberService _orderNumbers;
        private readonly YocoService _yoco;

        public CheckoutController(
            CartService cart,
            AppDbContext db,
            OrderNumberService orderNumbers,
            YocoService yoco)
        {
            _cart = cart;
            _db = db;
            _orderNumbers = orderNumbers;
            _yoco = yoco;
        }

        public IActionResult Index()
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any()) return RedirectToAction("Index", "Cart");
            return View(new CheckoutViewModel { Cart = cart });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel vm)
        {
            var cart = _cart.GetCart();
            if (!cart.Items.Any()) return RedirectToAction("Index", "Cart");
            vm.Cart = cart;
            if (!ModelState.IsValid) return View("Index", vm);

            // Verify stock
            foreach (var item in cart.Items)
            {
                var variant = await _db.ProductVariants
                    .FirstOrDefaultAsync(v =>
                        v.ProductId == item.ProductId && v.Size == item.Size);

                if (variant == null || variant.StockQuantity < item.Quantity)
                {
                    ModelState.AddModelError("",
                        $"{item.ProductName} ({item.Size}) is out of stock.");
                    return View("Index", vm);
                }
            }

            // Save a PENDING order first so we have the order number
            string? userId = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _db.Users
                    .FirstOrDefaultAsync(u => u.Email == User.Identity.Name);
                userId = user?.Id;
            }

            var orderNumber = _orderNumbers.Generate();

            var order = new Order
            {
                OrderNumber = orderNumber,
                UserId = userId,
                CustomerName = vm.FullName,
                CustomerEmail = vm.Email,
                ShippingAddress = vm.Address,
                City = vm.City,
                PostalCode = vm.PostalCode,
                Country = vm.Country,
                TotalAmount = cart.Subtotal,
                Status = OrderStatus.Pending,
                Items = cart.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Size = i.Size,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    ImagePath = i.ImagePath
                }).ToList(),
                StatusHistory = new List<OrderStatusHistory>
            {
                new() { Status = OrderStatus.Pending,
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = "System" }
            }
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();

            // Build redirect URLs
            var baseUrl    = $"{Request.Scheme}://{Request.Host}";
            var successUrl = $"{baseUrl}/checkout/payment-success?orderId={order.Id}";
            var cancelUrl = $"{baseUrl}/checkout/payment-cancelled?orderId={order.Id}";

            // Create Yoco checkout session
            var yocoSession = await _yoco.CreateCheckoutAsync(
                cart.Subtotal, orderNumber, successUrl, cancelUrl);

            if (yocoSession == null)
            {
                TempData["Error"] = "Payment could not be initiated. Please try again.";
                return View("Index", vm);
            }

            // Clear cart and redirect to Yoco hosted payment page
            _cart.ClearCart();
            return Redirect(yocoSession.RedirectUrl);
        }

        // Yoco redirects here after successful payment
        [Route("checkout/payment-success")]
        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) return NotFound();

            // Deduct stock now that payment is confirmed
            foreach (var item in order.Items)
            {
                var variant = await _db.ProductVariants
                    .FirstOrDefaultAsync(v =>
                        v.ProductId == item.ProductId && v.Size == item.Size);
                if (variant != null)
                    variant.StockQuantity -= item.Quantity;
            }

            // Update order status to Processing
            order.Status = OrderStatus.Processing;
            order.UpdatedAt = DateTime.UtcNow;
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = OrderStatus.Processing,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = "Yoco Payment"
            });

            await _db.SaveChangesAsync();

            return RedirectToAction("Confirmation", new { id = order.Id });
        }

        // Yoco redirects here if customer cancels payment
        [Route("checkout/payment-cancelled")]
        public async Task<IActionResult> PaymentCancelled(int orderId)
        {
            var order = await _db.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = OrderStatus.Cancelled;
                await _db.SaveChangesAsync();
            }
            TempData["Error"] = "Payment was cancelled. Your order has not been placed.";
            return RedirectToAction("Index", "Cart");
        }

        public async Task<IActionResult> Confirmation(int id)
        {
            var order = await _db.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == id);
            if (order == null) return NotFound();
            return View(new OrderConfirmationViewModel { Order = order });
        }
    }

    [Authorize]
    public class OrderController : Controller
    {
        private readonly AppDbContext _db;

        public OrderController(AppDbContext db) => _db = db;

        public async Task<IActionResult> MyOrders()
        {
            var userEmail = User.Identity!.Name;
            var orders = await _db.Orders
                .Where(o => o.CustomerEmail == userEmail)
                .Include(o => o.Items)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return View(orders);
        }

        public async Task<IActionResult> Detail(int id)
        {
            var userEmail = User.Identity!.Name;
            var order = await _db.Orders
                .Include(o => o.Items)
                .Include(o => o.StatusHistory.OrderByDescending(h => h.ChangedAt))
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerEmail == userEmail);

            if (order == null) return NotFound();
            return View(order);
        }
    }
}
