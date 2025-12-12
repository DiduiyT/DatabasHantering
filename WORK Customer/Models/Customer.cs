using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // TEXT: Customer representerar en kund i systemet med kontaktuppgifter och lösenord
    public class Customer
    {
        // TEXT: Primärnyckel för Customer
        public int CustomerId { get; set; }

        // TEXT: Kundens förnamn
        [Required]
        public string CustomerName { get; set; } = null!;

        // TEXT: Kundens efternamn
        [Required]
        public string LastName { get; set; } = null!;

        // TEXT: Kundens e-postadress (används för kontakt och identifiering)
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        // TEXT: Lösenord lagrat i klartext i denna demo (OBS - ej säkert i verkliga system)
        [Required]
        public string Password { get; set; } = null!;
    }
}