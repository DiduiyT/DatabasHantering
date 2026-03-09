using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // Modell för produktkategori. Stöder parent/sub-kategorier via self-referens.
    public class Category
    {
        // Primärnyckel för kategorin
        public int CategoryId { get; set; }

        // Kategorinamn
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        // Valfri FK till parent-kategori (om detta är en underkategori)
        public int? CategoryId1 { get; set; }
        public Category? Parent { get; set; }

        // Samling av underkategorier
        public ICollection<Category> Categories { get; set; } = new List<Category>();

        // Samling av produkter i denna kategori
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}