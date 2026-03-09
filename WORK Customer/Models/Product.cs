using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // Modell för en produkt i katalogen.
    public class Product
    {
        // Primärnyckel
        public int id { get; set; }

        // Produktens namn (obligatoriskt, max 200 tecken)
        [Required]
        [MaxLength(200)]
        public string name { get; set; } = null!;

        // Produktens pris
        [Required]
        public decimal price { get; set; }

        // FK till kategori och navigeringsproperty
        public int CategoryID { get; set; }
        public Category Category { get; set; } = null!;
    }
}