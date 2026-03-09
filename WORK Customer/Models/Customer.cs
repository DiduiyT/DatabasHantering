using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // Modell för en kund med kontaktuppgifter och lösenord (lagrat som hash).
    public class Customer
    {
        // Primärnyckel för kunden
        public int CustomerId { get; set; }

        // Kundens förnamn
        [Required]
        public string CustomerName { get; set; } = null!;

        // Kundens efternamn
        [Required]
        public string LastName { get; set; } = null!;

        // Kundens e-postadress (används för kontakt och som identifierare)
        [Required]
        [EmailAddress]
        public string Email { get; set; } = null!;

        // Lösenord lagras som en Base64-sträng som innehåller salt+hash.
        [Required]
        public string PasswordHash { get; set; } = null!;
    }
}