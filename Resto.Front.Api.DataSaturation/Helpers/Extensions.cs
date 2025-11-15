using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Interfaces;
using Resto.Front.Api.DataSaturation.Services;
using System;
using System.Collections.Concurrent;
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

        public static List<ProductInfoShort> GetProductInfoShort(this IProduct product, ProductInfo stopList = null) 
        { 
            List<ProductInfoShort> productInfoShorts = new List<ProductInfoShort>();
            if (product is null)
                return productInfoShorts;
              bool inStopList = false;

            var prod = product.GetProductInfo();
            if (stopList != null && prod.productSize?.Any() != true)
                inStopList = true;

            if (prod.productSize != null)
            {
                foreach (var item in prod.productSize)
                {
                    if (stopList != null)
                    {
                        foreach (var item1 in stopList.productSize)
                        {
                            if (string.Equals(item1.name, item.name, StringComparison.OrdinalIgnoreCase))
                            {
                                inStopList = true;
                            }
                        }
                    }
                    ProductInfoShort productInfoWithSize = new ProductInfoShort()
                    {
                        id = $"{prod.barcode}_{item.name}",
                        name = $"{prod.name}_{item.name}",
                        price = item.price,
                        inStopList = inStopList
                    };
                    productInfoShorts.Add(productInfoWithSize);
                    PluginContext.Log.Info(productInfoWithSize.SerializeToJson());
                }
                return productInfoShorts;
            }

                ProductInfoShort productInfo = new ProductInfoShort()
            {
                id = prod.barcode,
                name = prod.name,
                price = prod.price,
                inStopList = inStopList
            };
            productInfoShorts.Add(productInfo);
			PluginContext.Log.Info(productInfo.SerializeToJson());
            return productInfoShorts;
        }

        public static List<ProductInfoShort> GetProductInfoByStopList(this ConcurrentDictionary<Guid, ProductInfo> stopList)
        {
            List<ProductInfoShort> productInfoShorts = new List<ProductInfoShort>();
            foreach (var prod in stopList.Values)
            {
                ProductInfoShort productInfoShort;
                if (prod.productSize != null)
                {
                    foreach (var item in prod.productSize)
                    {
                        productInfoShort = new ProductInfoShort()
                        {
                            id = $"{prod.barcode}_{item?.name}",
                            name = $"{prod.name}_{item?.name}",
                            price = item != null ? item.price : 0,
                            inStopList = true
                        };

                        PluginContext.Log.Info(productInfoShort.SerializeToJson());
                        productInfoShorts.Add(productInfoShort);
                    }
                }
                else
                {
                    productInfoShort = new ProductInfoShort()
                    {
                        id = prod.barcode,
                        name = prod.name,
                        price = prod.price,
                        inStopList = true   
                    };
                    PluginContext.Log.Info(productInfoShort.SerializeToJson());
                    productInfoShorts.Add(productInfoShort);
                }
            }
            return productInfoShorts;
        }

        public static void AddValuesToSendData(this ProductInfoShortApi toSendData, List<ProductInfoShort> productInfo)
        {
            if (toSendData == null)
                return;

            if (toSendData.items is null)
                toSendData.items = new List<ProductInfoShort>();

            if (toSendData.currentStopList == null) 
                toSendData.currentStopList = new List<ProductInfoShort>();

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
                if (order.Status == OrderStatus.Bill || order.Status == OrderStatus.Closed)
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
                        var groupModifier = ModifiersService.Instance.GetGroupModifierInfo(productItem.Product, modifier.Product.Id);
                        var modifierItem = GetOrderModifierInfo(modifier, groupModifier);
                        if (modifierItem != null)
                            productInfo.modifiers.Add(modifierItem);
                    }

                    productInfos.Add(productItem.Id, productInfo);
                    continue;
                }
                if (!(item is IOrderCompoundItem compoundItem))
                    continue;

                AddOrderCompoundItem(productInfos, compoundItem, order.PriceCategory);
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

        private static void AddOrderCompoundItem(Dictionary<Guid, OrderItemInfo> productInfos, IOrderCompoundItem compoundItem, IPriceCategory priceCategory)
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

        private static OrderItemInfo FillCompoundComponent(IOrderCompoundItem compoundItem, IOrderCompoundItemComponent component, IPriceCategory priceCategory)
        {
            var sizeComponent = GetProductSize(component.Product, compoundItem.Size);

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
        private static OrderModifierInfo GetOrderModifierInfo(IOrderModifierItem orderModifierItem, IChildModifier childModifier)
        {
            if (childModifier != null)
            {
                if (childModifier.DefaultAmount == orderModifierItem.Amount)
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
