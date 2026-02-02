using System;

namespace aim.legacy.domain
{
    /// <summary>
    /// Represents a single line item within an order.
    /// Contains product reference, quantity, and pricing information.
    /// </summary>
    public class OrderLine
    {
        public long? Id { get; set; }
        public long? ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }

        public OrderLine()
        {
        }

        public OrderLine(long? id, long? productId, string productName, int quantity, decimal unitPrice)
        {
            Id = id;
            ProductId = productId;
            ProductName = productName ?? "";
            Quantity = quantity;
            UnitPrice = unitPrice;
        }

        /// <summary>
        /// Calculates the total for this line item.
        /// Returns quantity multiplied by unit price.
        /// </summary>
        public decimal LineTotal => UnitPrice * Quantity;

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not OrderLine other) return false;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"{ProductName} x{Quantity} @ ${UnitPrice:N2} = ${LineTotal:N2}";
        }
    }
}
