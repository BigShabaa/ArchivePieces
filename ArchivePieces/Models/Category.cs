using System.ComponentModel.DataAnnotations;

namespace ArchivePieces.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string Slug { get; set; } = string.Empty;

        public int DisplayOrder { get; set; } = 0;

        public bool IsVisible { get; set; } = true;

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
