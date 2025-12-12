using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // TEXT: Product representerar en produkt i katalogen
    public class Product
    {
        // TEXT: Primärnyckel (id)
        public int id { get; set; }

        // TEXT: Produktens namn (required, max 200)
        [Required]
        [MaxLength(200)]
        public string name { get; set; } = null!;

        // TEXT: Produktens pris
        [Required]
        public decimal price { get; set; }

        // TEXT: Foreign key till Category
        public int CategoryID { get; set; }
        public Category Category { get; set; } = null!;
    }
}