using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Domain.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Resto.Front.Api.DataSaturation.Domain.Helpers
{
    public static class OrderExtensions
    {
        public static OrderInfo OrderToOrderInfo(this IOrder order, EntityEventType eventType, string dataQR)
        {
            OrderStatusInfo orderStatus = OrderStatusInfo.start;
            if (eventType == EntityEventType.Updated)
            {
                if (order.Status == OrderStatus.Bill || order.Status == OrderStatus.Closed || order.Items.Count == 0)
                    orderStatus = OrderStatusInfo.close;
                else
                    orderStatus = OrderStatusInfo.update;
            }
            Dictionary<Guid, OrderItemInfo> productInfos = new Dictionary<Guid, OrderItemInfo>();
            foreach (var item in order.Items)
            {
                if (productInfos.ContainsKey(item.Id))
                    continue;
                if (item is IOrderProductItem productItem)
                {
                    var size = ProductHelper.GetProductSize(productItem.Product, productItem.Size);
                    var countProduct = productItem.Amount;
                    var productInfo = new OrderItemInfo
                    {
                        id = productItem.Id,
                        deleted = productItem.Deleted,
                        name = productItem.Product?.Name,
                        price = productItem.Price,
                        productSize = size,
                        amount = productItem.Amount,
                        printTime = item.PrintTime,
                        modifiers = new List<OrderModifierInfo>()
                    };
                    foreach (var modifier in productItem.AssignedModifiers)
                    {
                        var groupModifier = ModifiersService.Instance.GetGroupModifierInfo(productItem.Product, modifier.Product.Id);
                        var modifierItem = OrderHelper.GetOrderModifierInfo(modifier, groupModifier, countProduct);
                        if (modifierItem != null)
                            productInfo.modifiers.Add(modifierItem);
                    }

                    productInfos.Add(productItem.Id, productInfo);
                    continue;
                }
                if (!(item is IOrderCompoundItem compoundItem))
                    continue;

                OrderHelper.AddOrderCompoundItem(productInfos, compoundItem, order.PriceCategory);
            }

            return new OrderInfo
            {
                id = order.Id,
                ClientName = order.Guests.FirstOrDefault()?.Name,
                ClientBalance = PluginContext.Operations.TryGetOrderExternalDataByKey(order, Constants.ExternalDataKeyCustomerBalance),
                orderNumber = order.Number,
                orderStatus = orderStatus,
                items = productInfos.Values.OrderBy(item => item.printTime).ToList(),
                sumWithoutDiscounts = order.FullSum,
                sumWithDiscounts = order.ResultSum,
                visibleQR = OrderHelper.PaymantsByQR(order, orderStatus),
                dataQR = dataQR
            };
        }

    }
}
