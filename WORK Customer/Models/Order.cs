using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    // TEXT: Order representerar en kundorder med skapelsedatum och en samling OrderRow
    public class Order
    {
        // TEXT: Primärnyckel för Order
        public int OrderId { get; set; }

        // TEXT: FK till Customer och navigeringsproperty
        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        // TEXT: Tidpunkt då order skapades
        public DateTime CreatedAt { get; set; }

        // TEXT: Kollektion med rader (OrderRow) som tillhör ordern
        public ICollection<OrderRow> Rows { get; set; } = new List<OrderRow>();

        // NOTE: OrderDate togs bort eftersom CreatedAt används
    }
}