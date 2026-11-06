namespace ArchivePieces.Models
{
    public class Cart
    {
        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public int TotalItems => Items.Sum(i => i.Quantity);
        public decimal Subtotal => Items.Sum(i => i.LineTotal);

        public void AddItem(CartItem item)
        {
            var existing = Items.FirstOrDefault(i =>
                i.ProductId == item.ProductId && i.Size == item.Size);

            if (existing != null)
                existing.Quantity += item.Quantity;
            else
                Items.Add(item);
        }

        public void RemoveItem(int productId, string size)
        {
            Items.RemoveAll(i => i.ProductId == productId && i.Size == size);
        }

        public void UpdateQuantity(int productId, string size, int quantity)
        {
            var item = Items.FirstOrDefault(i =>
                i.ProductId == productId && i.Size == size);

            if (item != null)
            {
                if (quantity <= 0)
                    Items.Remove(item);
                else
                    item.Quantity = quantity;
            }
        }

        public void Clear() => Items.Clear();
    }

    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSlug { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImagePath { get; set; }
        public decimal LineTotal => UnitPrice * Quantity;
    }
}
