using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Resto.Front.Api.DataSaturation.Helpers
{
    public static class Extensions
    {
        public static ProductInfo GetProductInfo(this IProduct product)
        {
            var productInfo = new ProductInfo()
            {
                id = product.Id,
                name = product.Name,
                barcode = product.Number,
                price = product.Price,
                scale_id = product.Scale?.Id,
                menuIndex = product.MenuIndex
            };
            if (product.Scale != null && product.Id != null)
            {
                productInfo.productSize = new List<ProductSize>();
                var productSizeScale = PluginContext.Operations.GetProductScaleSizes(product.Scale);
                foreach (var item in productSizeScale)
                {
                    var size = GetProductSize(product, item);
                    if (size is null)
                        continue;

                    productInfo.productSize.Add(size);
                }

            }
            return productInfo;
        }

        public static List<ProductInfoShort> GetProductInfoShort(this IProduct product) 
        { 
            List<ProductInfoShort> productInfoShorts = new List<ProductInfoShort>();
            if (product is null)
                return productInfoShorts;

            var prod = product.GetProductInfo();
            if (prod.productSize != null)
            {
                foreach (var item in prod.productSize)
                {
                    ProductInfoShort productInfoWithSize = new ProductInfoShort()
                    {
                        id = $"{prod.id}_{prod.barcode}_{item.name}",
                        name = $"{prod.name}_{item.name}",
                        price = item.price
                    };
                    productInfoShorts.Add(productInfoWithSize);
                    PluginContext.Log.Info(productInfoWithSize.SerializeToJson());
                }
                return productInfoShorts;
            }

            ProductInfoShort productInfo = new ProductInfoShort()
            {
                id = $"{prod.id}_{prod.barcode}_{prod.name}",
                name = prod.name,
                price = prod.price
            };
            productInfoShorts.Add(productInfo);
            return productInfoShorts;
        }

        public static void AddValuesToSendData(this ProductInfoShortApi toSendData, List<ProductInfoShort> productInfo)
        {
            if (toSendData == null)
                return;

            if (toSendData.items is null)
                toSendData.items = new List<ProductInfoShort>();

            foreach (var item in productInfo)
            {
                if (item is null)
                    continue;

                toSendData.items.Add(item);
            }
        }

        private static ProductSize GetProductSize(IProduct product, IProductSize productSize)
        {
            if (product is null || productSize is null)
                return null;

            var price = PluginContext.Operations.GetPrice(product, productSize, null, DateTime.Now);

            return new ProductSize()
            {
                id = product.Id,
                name = productSize.Name,
                price = price
            };
        }

        public static OrderInfo OrderToOrderInfo(this IOrder order, EntityEventType eventType)
        {
            OrderStatusInfo orderStatus = OrderStatusInfo.start;
            if (eventType == EntityEventType.Updated)
            {
                if (order.Status == OrderStatus.Closed)
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
                    var size = GetProductSize(productItem.Product, productItem.Size);
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
                        productInfo.modifiers.Add(new OrderModifierInfo
                        {
                            id = modifier.Id,
                            amount = modifier.Amount,
                            deleted = modifier.Deleted,
                            name = modifier.Product?.Name,
                            price = modifier.Price,
                        });
                    }

                    productInfos.Add(productItem.Id, productInfo);
                    continue;
                }
                if (!(item is IOrderCompoundItem compoundItem))
                    continue;

                AddOrderCompoundItem(productInfos, compoundItem);
            }

            return new OrderInfo
            {
                id = order.Id,
                orderNumber = order.Number,
                orderStatus = orderStatus,
                items = productInfos.Values.OrderBy(item => item.printTime).ToList(),
                sum = order.FullSum
            };
        }

        private static void AddOrderCompoundItem(Dictionary<Guid, OrderItemInfo> productInfos, IOrderCompoundItem compoundItem)
        {
            if (compoundItem == null)
                return;

            if (compoundItem.PrimaryComponent != null && !productInfos.ContainsKey(compoundItem.PrimaryComponent.Id))
                productInfos.Add(compoundItem.PrimaryComponent.Id, FillCompoundComponent(compoundItem, compoundItem.PrimaryComponent));

            if (compoundItem.SecondaryComponent is null)
                return;

            if (productInfos.ContainsKey(compoundItem.SecondaryComponent.Id))
                return;

            productInfos.Add(compoundItem.SecondaryComponent.Id, FillCompoundComponent(compoundItem, compoundItem.SecondaryComponent));
        }

        private static OrderItemInfo FillCompoundComponent(IOrderCompoundItem compoundItem, IOrderCompoundItemComponent component)
        {
            var sizeSecondComponent = GetProductSize(component.Product, compoundItem.Size);
            List<OrderModifierInfo> modifierInfos = new List<OrderModifierInfo>();
            foreach (var modifier in component.Modifiers)
            {
                modifierInfos.Add(new OrderModifierInfo
                {
                    amount = modifier.Amount,
                    deleted = modifier.Deleted,
                    id = modifier.Id,
                    name = modifier.Product?.Name,
                    price = modifier.Price
                });
            }
            return new OrderItemInfo
            {
                id = component.Id,
                deleted = compoundItem.Deleted,
                name = component.Product?.Name,
                price = component.Price,
                productSize = sizeSecondComponent,
                amount = compoundItem.Amount
            };
        }

        public static bool IsEqualsLists<T>(List<T> firstCollection, List<T> secondCollection) where T : IEqualsObject
        {
            if (firstCollection is null && secondCollection != null)
                return false;

            if (firstCollection != null && secondCollection is null)
                return false;

            if (firstCollection is null && secondCollection is null)
                return true;

            if (firstCollection.Count != secondCollection.Count)
                return false;

            Dictionary<Guid, T> currentModifiers = new Dictionary<Guid, T>();
            foreach (var product in firstCollection)
            {
                if (currentModifiers.ContainsKey(product.id))
                    continue;

                currentModifiers.Add(product.id, product);
            }

            foreach (var product in secondCollection)
            {
                if (!currentModifiers.TryGetValue(product.id, out T orderModifierItem))
                    return false;

                if (!orderModifierItem.Equals(product))
                    return false;
            }
            return true;
        }
    }
}
