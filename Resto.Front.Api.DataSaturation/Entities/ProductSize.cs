using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class ProductSize : IEqualsObject
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is null || !(obj is ProductSize productSize))
                return false;
            
            if (id != productSize.id ||
                string.Equals(name, productSize.name, StringComparison.OrdinalIgnoreCase) ||
                price != productSize.price)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, name, price);
        }
    }
}
