using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    public class Product
    {
        public int id { get; set; }

        [Required]
        [MaxLength(200)]
        public string name { get; set; } = null!;

        [Required]
        public decimal price { get; set; }

        // FK to Category
        public int CategoryID { get; set; }
        public Category Category { get; set; } = null!;
    }
}