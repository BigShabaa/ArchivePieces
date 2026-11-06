using ArchivePieces.Models;
using System.Text.Json;

namespace ArchivePieces.Services
{
    public class CartService
    {
        private const string CartKey = "AP_CART";
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public Cart GetCart()
        {
            var json = Session.GetString(CartKey);
            return json == null
                ? new Cart()
                : JsonSerializer.Deserialize<Cart>(json) ?? new Cart();
        }

        private void SaveCart(Cart cart)
        {
            Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        }

        public void AddItem(CartItem item)
        {
            var cart = GetCart();
            cart.AddItem(item);
            SaveCart(cart);
        }

        public void RemoveItem(int productId, string size)
        {
            var cart = GetCart();
            cart.RemoveItem(productId, size);
            SaveCart(cart);
        }

        public void UpdateQuantity(int productId, string size, int quantity)
        {
            var cart = GetCart();
            cart.UpdateQuantity(productId, size, quantity);
            SaveCart(cart);
        }

        public void ClearCart()
        {
            SaveCart(new Cart());
        }

        public int GetItemCount() => GetCart().TotalItems;
    }
}
