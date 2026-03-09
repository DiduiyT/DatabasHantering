using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // Modell för en kundorder med skapelsedatum och en samling orderrader.
    public class Order
    {
        // Primärnyckel för ordern
        public int OrderId { get; set; }

        // FK till kund och navigeringsproperty
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // Tidpunkt då ordern skapades
        public DateTime CreatedAt { get; set; }

        // Samling med rader (OrderRow) som tillhör denna order
        public ICollection<OrderRow> Rows { get; set; } = new List<OrderRow>();

        // NOTE: OrderDate togs bort eftersom CreatedAt används
    }
}