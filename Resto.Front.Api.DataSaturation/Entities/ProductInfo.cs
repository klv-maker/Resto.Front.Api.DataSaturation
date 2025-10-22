using Resto.Front.Api.Data.Assortment;
using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class ProductInfo
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string barcode { get; set; }
        public decimal price { get; set; }
        public Guid? scale_id { get; set; }
        public int menuIndex { get; set; }
        public List<ProductSize> productSize { get; set; }
    }
}
