using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Domain.Entities;
using System;
using System.Linq;

namespace Resto.Front.Api.DataSaturation.Domain.Helpers
{
    public static class ProductHelper
    {
        public static ProductSize GetProductSize(IProduct product, IProductSize productSize)
        {
            if (product is null || productSize is null)
                return null;
            var disabledSizes = PluginContext.Operations.TryGetDisabledSizesByProduct(product);

            if (!disabledSizes.Any(_ => _.Name == productSize.Name))
            {
                var price = PluginContext.Operations.GetPrice(product, productSize, null, DateTime.Now);

                return new ProductSize()
                {
                    id = product.Id,
                    name = productSize.Name,
                    price = price
                };
            }
            else
            {
                return new ProductSize()
                {
                    id = product.Id,
                    name = productSize.Name,
                    price = 0
                };
            }
        }
    }
}
