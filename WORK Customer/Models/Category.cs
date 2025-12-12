using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // TEXT: Category representerar en produktkategori, stödjer self?reference för parent/subcategories
    public class Category
    {
        // TEXT: Primärnyckel för Category
        public int CategoryId { get; set; }

        // TEXT: Kategorinamn
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        // TEXT: Valfri FK till parentkategori
        public int? CategoryId1 { get; set; }
        public Category? Parent { get; set; }

        // TEXT: Samling av underkategorier
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        // TEXT: Samling av produkter i denna kategori
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}