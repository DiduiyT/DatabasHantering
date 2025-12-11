using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WORK_Customer
{
    public class Order
    {
        public int OrderId { get; set; }

        public int CustomerId { get; set; }
        public Customer Customer { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public ICollection<OrderRow> Rows { get; set; } = new List<OrderRow>();

        [Required]
        public DateTime OrderDate { get; set; }

    }
}