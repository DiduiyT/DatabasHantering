using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    public class Customer
    {
        public int CustomerId { get; set; }

        [Required]
        public string CustomerName { get; set; } = null!;

        [Required]
        public string LastName { get; set; } = null!;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        // Plain password stored for demo (not recommended for production)
        [Required]
        public string Password { get; set; } = null!;
    }
}