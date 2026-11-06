using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ArchivePieces.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required, MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(200)]
        public string Slug { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required, Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        // Computed helpers
        public bool IsNew => (DateTime.UtcNow - CreatedAt).TotalDays <= 14;
        public bool IsSoldOut => !Variants.Any(v => v.StockQuantity > 0);
        public int TotalStock => Variants.Sum(v => v.StockQuantity);
        public string? PrimaryImagePath => Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImagePath;
    }

    public class ProductImage
    {
        public int Id { get; set; }

        [Required]
        public string ImagePath { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
    }

    public class ProductVariant
    {
        public int Id { get; set; }

        [Required, MaxLength(10)]
        public string Size { get; set; } = string.Empty;  // XS, S, M, L, XL, XXL

        public int StockQuantity { get; set; } = 0;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public bool IsLowStock => StockQuantity > 0 && StockQuantity <= 3;
        public bool IsOutOfStock => StockQuantity <= 0;
    }
}
