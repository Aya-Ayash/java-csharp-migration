using System;

namespace aim.legacy.domain
{
    /// <summary>
    /// Represents a customer entity in the order management system.
    /// Contains all customer contact information.
    /// </summary>
    public class Customer
    {
        public long? Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Address { get; set; } = "";
        public string CustomerType { get; set; } = ""; // STANDARD, PREMIUM, VIP

        public Customer()
        {
        }

        public Customer(long? id, string name, string email, string phone, string address)
        {
            Id = id;
            Name = name ?? "";
            Email = email ?? "";
            Phone = phone ?? "";
            Address = address ?? "";
        }

        public Customer(long? id, string name, string email, string phone, string address, string customerType)
        {
            Id = id;
            Name = name ?? "";
            Email = email ?? "";
            Phone = phone ?? "";
            Address = address ?? "";
            CustomerType = customerType ?? "";
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (obj is not Customer other) return false;
            return Id == other.Id;
        }

        public override int GetHashCode()
        {
            return Id?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"{Name} ({Email})";
        }
    }
}
