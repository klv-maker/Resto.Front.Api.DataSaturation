using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class OrderInfo : IEqualsObject
    {
        public Guid id { get; set; }
        public int orderNumber { get; set; }
        public OrderStatusInfo orderStatus { get; set; }
        public List<OrderItemInfo> items { get; set; }
        public decimal sum { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is OrderInfo order)) 
                return false;

            if (order.id != id ||
                order.orderStatus != orderStatus ||
                order.sum != sum ||
                order.orderNumber != orderNumber)
                return false;

            if (!Helpers.Extensions.IsEqualsLists(items, order.items))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, orderNumber, orderStatus, items, sum);
        }
    }
}
