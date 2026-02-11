using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Domain.Services;
using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Domain.Helpers
{
    public static class OrderHelper
    {
        public static bool PaymantsByQR(IOrder order, OrderStatusInfo orderStatus)
        {
            var result = false;
            if (order.Payments.Count > 0)
            {
                foreach (var item in order.Payments)
                {
                    if ((item.Type.Name.IndexOf("яндекс", StringComparison.OrdinalIgnoreCase) >= 0) || (item.Type.Name.IndexOf("yandex", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        if (!item.AdditionalData.SerializeToJson().Contains("PayLink") && item.Sum > 0)
                        {
                            result = true;
                        }
                    }
                }
            }
            return result;
        }

        public static void AddOrderCompoundItem(Dictionary<Guid, OrderItemInfo> productInfos, IOrderCompoundItem compoundItem, IPriceCategory priceCategory)
        {
            if (compoundItem == null)
                return;

            if (compoundItem.PrimaryComponent != null && !productInfos.ContainsKey(compoundItem.PrimaryComponent.Id))
                productInfos.Add(compoundItem.PrimaryComponent.Id, FillCompoundComponent(compoundItem, compoundItem.PrimaryComponent, priceCategory));

            if (compoundItem.SecondaryComponent is null)
                return;

            if (productInfos.ContainsKey(compoundItem.SecondaryComponent.Id))
                return;

            productInfos.Add(compoundItem.SecondaryComponent.Id, FillCompoundComponent(compoundItem, compoundItem.SecondaryComponent, priceCategory));
        }

        public static OrderItemInfo FillCompoundComponent(IOrderCompoundItem compoundItem, IOrderCompoundItemComponent component, IPriceCategory priceCategory)
        {
            var sizeComponent = ProductHelper.GetProductSize(component.Product, compoundItem.Size);

            List<OrderModifierInfo> modifierInfos = new List<OrderModifierInfo>();
            foreach (var modifier in component.Modifiers)
            {
                var groupModifier = ModifiersService.Instance.GetGroupModifierInfo(component.Product, modifier.Id);
                var modifierItem = GetOrderModifierInfo(modifier, groupModifier);
                if (modifierItem != null)
                    modifierInfos.Add(modifierItem);
            }
            return new OrderItemInfo
            {
                id = component.Id,
                deleted = compoundItem.Deleted,
                name = component.Product?.Name,
                price = component.Price,
                productSize = sizeComponent,
                amount = compoundItem.Amount,
                modifiers = modifierInfos
            };
        }
        public static OrderModifierInfo GetOrderModifierInfo(IOrderModifierItem orderModifierItem, IChildModifier childModifier, decimal countProduct = 1)
        {
            if (childModifier != null)
            {
                if (childModifier.DefaultAmount * countProduct == orderModifierItem.Amount)
                {
                    if (!orderModifierItem.Deleted && childModifier.HideIfDefaultAmount)
                        return null;

                    return new OrderModifierInfo
                    {
                        amount = orderModifierItem.Amount,
                        deleted = orderModifierItem.Deleted,
                        id = orderModifierItem.Id,
                        name = orderModifierItem.Product?.Name,
                        price = orderModifierItem.Price
                    };
                }
            }

            return new OrderModifierInfo
            {
                amount = orderModifierItem.Amount,
                deleted = orderModifierItem.Deleted,
                id = orderModifierItem.Id,
                name = orderModifierItem.Product?.Name,
                price = orderModifierItem.Price
            };
        }
    }
}
