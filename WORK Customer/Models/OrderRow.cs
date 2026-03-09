using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // Modell för en rad i en order: produkt, kvantitet och pris vid beställningstillfället.
    public class OrderRow
    {
        // Primärnyckel för orderraden
        public int OrderRowId { get; set; }

        // FK till Order och navigeringsproperty
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        // FK till produkt och navigeringsproperty
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // Antal enheter i denna rad
        public int Quantity { get; set; }

        // Enhetspris kopierat från produkten när ordern skapades (för pris-historik)
        public decimal UnitPrice { get; set; }
    }
}