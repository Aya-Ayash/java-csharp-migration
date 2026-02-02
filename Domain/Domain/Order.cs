using System;
using System.Collections.Generic;

namespace aim.legacy.domain
{
    /// <summary>
    /// Represents a customer order with line items.
    /// Manages order totals, discounts, and tax calculations.
    /// </summary>
    public class Order
    {
        public long? Id { get; set; }
        public long? CustomerId { get; set; }
        public string CustomerName { get; set; } = "";
        public DateTime OrderDate { get; set; }
        public List<OrderLine> Lines { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }

        public Order()
        {
            Lines = new List<OrderLine>();
            OrderDate = DateTime.Now;
        }

        public Order(long? id, long? customerId, string customerName)
            : this()
        {
            Id = id;
            CustomerId = customerId;
            CustomerName = customerName ?? "";
        }

        public void AddLine(OrderLine line)
        {
            Lines.Add(line);
        }

        public void RemoveLine(OrderLine line)
        {
            Lines.Remove(line);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not Order other) return false;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"Order #{Id} - {CustomerName} - Total: ${Total:N2}";
        }
    }
}
