using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Domain.Interfaces;
using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Domain.Entities
{
    public class OrderInfo : IEqualsObject
    {
        public Guid id { get; set; }
        public string ClientName { get; set; }
        public string ClientBalance { get; set; }
        public int orderNumber { get; set; }
        public OrderStatusInfo orderStatus { get; set; }
        public List<OrderItemInfo> items { get; set; }
        public decimal sumWithoutDiscounts { get; set; }
        public decimal sumWithDiscounts { get; set; }
        public bool visibleQR { get; set; }
        public string dataQR { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is OrderInfo order)) 
                return false;

            if (order.id != id ||
                order.orderStatus != orderStatus ||
                order.sumWithoutDiscounts != sumWithoutDiscounts ||
                order.sumWithDiscounts != sumWithDiscounts ||
                order.orderNumber != orderNumber)
                return false;

            if (!CollectionsHelper.IsEqualsLists(items, order.items))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, orderNumber, orderStatus, items, sumWithoutDiscounts);
        }
    }
}
