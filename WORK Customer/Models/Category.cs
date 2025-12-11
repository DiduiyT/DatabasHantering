using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    public class Category
    {
        public int CategoryId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        // Optional parent category (self reference)
        public int? CategoryId1 { get; set; }
        public Category? Parent { get; set; }

        // Subcategories
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        // Products in this category
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}