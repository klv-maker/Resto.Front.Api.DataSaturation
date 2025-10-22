using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Entities;
using System;
using System.Collections.Generic;

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
                    var price = PluginContext.Operations.GetPrice(product, item, null, DateTime.Now);

                    var size = new ProductSize()
                    {
                        id = product.Id,
                        name = item.Name,
                        price = price
                    };
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
    }
}
