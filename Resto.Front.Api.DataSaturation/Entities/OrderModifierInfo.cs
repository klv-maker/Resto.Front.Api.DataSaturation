using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class OrderModifierInfo : IEqualsObject
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public decimal amount { get; set; }
        public decimal price { get; set; }
        public bool deleted { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is null || !(obj is OrderModifierInfo modifierInfo))
                return false;

            if (id != modifierInfo.id ||
                string.Equals(name, modifierInfo.name, StringComparison.OrdinalIgnoreCase) ||
                amount != modifierInfo.amount ||
                price != modifierInfo.price ||
                deleted != modifierInfo.deleted)
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, name, amount, price, deleted);
        }
    }
}
