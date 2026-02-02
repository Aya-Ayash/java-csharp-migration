using System;

namespace aim.legacy.domain
{
    /// <summary>
    /// Represents a product in the catalog.
    /// Includes pricing information and product details.
    /// </summary>
    public class Product
    {
        public long? Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal UnitPrice { get; set; }

        public Product()
        {
        }

        public Product(long? id, string name, string description, decimal unitPrice)
        {
            Id = id;
            Name = name ?? "";
            Description = description ?? "";
            UnitPrice = unitPrice;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not Product other) return false;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"{Name} - ${UnitPrice:N2}";
        }
    }
}
