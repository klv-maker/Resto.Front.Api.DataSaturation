using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Entities;
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

        public static List<ProductInfoShort> GetProductInfoShort(this IProduct product, IList<ProductAndSize> stopList = null) 
        { 
            List<ProductInfoShort> productInfoShorts = new List<ProductInfoShort>();
            if (product is null)
                return productInfoShorts;
              bool inStopList = false;
            if (stopList != null)
            {
                if (stopList.Any(_ => _.Product.Id == product.Id))
                {
                    inStopList = true;
                }
            }

            var prod = product.GetProductInfo();
            if (prod.productSize != null)
            {
                foreach (var item in prod.productSize)
                {
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

        public static List<ProductInfoShort> GetProductInfoByStopList(this IList<ProductAndSize> stopList)
        {
            List<ProductInfoShort> productInfoShorts = new List<ProductInfoShort>();
            ProductInfo productInfo = new ProductInfo();
            ProductSize productSize = new ProductSize();
            foreach (var prod in stopList)
            {
                productInfo = prod.Product.GetProductInfo();
                if (productInfo.productSize != null)
                {
                    productSize = productInfo.productSize.FirstOrDefault(_ => _.name == prod.ProductSize.Name);
                    ProductInfoShort productInfoWithSize = new ProductInfoShort()
                    {
                        id = $"{productInfo.barcode}_{productSize.name}",
                        name = $"{productInfo.name}_{productSize.name}",
                        price = productSize.price,
                        inStopList = true
                    };

                    productInfoShorts.Add(productInfoWithSize);
                    PluginContext.Log.Info(productInfoWithSize.SerializeToJson());
                }
                else
                {
                    ProductInfoShort productInfoShort = new ProductInfoShort()
                    {
                        id = productInfo.barcode,
                        name = productInfo.name,
                        price = productInfo.price,
                        inStopList = true   
                    };
                    productInfoShorts.Add(productInfoShort);
                    PluginContext.Log.Info(productInfoShort.SerializeToJson());
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

            foreach (var item in productInfo)
            {
                if (item is null)
                    continue;

                toSendData.items.Add(item);
            }
        }
    }
}
