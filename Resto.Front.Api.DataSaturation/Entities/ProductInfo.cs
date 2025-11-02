using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class ProductInfo : IEqualsObject
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string barcode { get; set; }
        public decimal price { get; set; }
        public Guid? scale_id { get; set; }
        public int menuIndex { get; set; }
        public List<ProductSize> productSize { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is ProductInfo productInfo)) 
                return false;

            if (id != productInfo.id ||
                string.Equals(name, productInfo.name, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(barcode, productInfo.barcode, StringComparison.OrdinalIgnoreCase) ||
                price != productInfo.price ||
                scale_id != productInfo.scale_id ||
                menuIndex != productInfo.menuIndex)
                return false;


            if (!Helpers.Extensions.IsEqualsLists(productSize, productInfo.productSize))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, name, barcode, price, scale_id, menuIndex, productSize);
        }
    }
}
