using ArchivePieces.Models;
using System.ComponentModel.DataAnnotations;

namespace ArchivePieces.ViewModels.Admin
{
    // ── Dashboard ──────────────────────────────────────────────────────────────
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int OutOfStockVariants { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
    }

    // ── Product create / edit ──────────────────────────────────────────────────
    public class ProductFormViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Slug { get; set; } = string.Empty;   // auto-generated if blank

        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, 999999)]
        public decimal Price { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;

        public List<IFormFile> NewImages { get; set; } = new();
        public List<ProductImage> ExistingImages { get; set; } = new();
        public List<int> DeleteImageIds { get; set; } = new();

        public List<VariantFormItem> Variants { get; set; } = new();

        public List<Category> Categories { get; set; } = new();
    }

    public class VariantFormItem
    {
        public int Id { get; set; }       // 0 = new
        public string Size { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public bool Delete { get; set; }
    }

    // ── Category create / edit ─────────────────────────────────────────────────
    public class CategoryFormViewModel
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; } = true;
    }

    // ── Order list ─────────────────────────────────────────────────────────────
    public class OrderListViewModel
    {
        public List<Order> Orders { get; set; } = new();
        public string? StatusFilter { get; set; }
        public string? Search { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; }
    }

    // ── Order detail ───────────────────────────────────────────────────────────
    public class OrderDetailViewModel
    {
        public Order Order { get; set; } = null!;
        public List<OrderStatus> AvailableStatuses { get; set; } = new();
    }

    // ── Stock management ───────────────────────────────────────────────────────
    public class StockViewModel
    {
        public List<StockRow> Rows { get; set; } = new();
        public string? Search { get; set; }
    }

    public class StockRow
    {
        public int VariantId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }
}
