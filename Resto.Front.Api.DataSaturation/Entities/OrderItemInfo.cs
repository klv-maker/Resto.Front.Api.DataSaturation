using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class OrderItemInfo : IEqualsObject
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public decimal amount { get; set; }
        public decimal price { get; set; }
        public bool deleted { get; set; }
        public DateTime? printTime { get; set; }
        public decimal sum 
        {
            get
            {
                return amount * price;
            }
        }
        public ProductSize productSize { get; set; }
        public List<OrderModifierInfo> modifiers { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is null || !(obj is OrderItemInfo orderItem))
                return false;

            if (id != orderItem.id ||
                string.Equals(name, orderItem.name, StringComparison.OrdinalIgnoreCase) ||
                deleted != orderItem.deleted ||
                price != orderItem.price ||
                sum != orderItem.sum ||
                amount != orderItem.amount ||
                productSize.Equals(orderItem.productSize) ||
                printTime != orderItem.printTime)
                return false;

            if (!Helpers.Extensions.IsEqualsLists(modifiers, orderItem.modifiers))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, name, deleted, price, modifiers, amount, productSize, printTime);
        }
    }
}
