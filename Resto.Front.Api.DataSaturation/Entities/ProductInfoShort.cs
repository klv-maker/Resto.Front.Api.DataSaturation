using System;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class ProductInfoShort
    {
        public string id { get; set; }
        public string name { get; set; }
        public decimal price { get; set; }
        public bool inStopList {  get; set; }
    }
}
