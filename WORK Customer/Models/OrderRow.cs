using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // TEXT: OrderRow representerar en rad i en order (enskild produkt + kvantitet och pris vid ordertillfället)
    public class OrderRow
    {
        // TEXT: Primärnyckel för orderraden
        public int OrderRowId { get; set; }

        // TEXT: FK till Order och navigeringsproperty till Order
        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        // TEXT: FK till Product och navigeringsproperty till Product
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        // TEXT: Antal enheter av produkten i denna rad
        public int Quantity { get; set; }

        // TEXT: Enhetspris som kopieras från produkt vid orderläggning (för att bevara pris history)
        public decimal UnitPrice { get; set; }
    }
}