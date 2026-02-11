using System;
using System.Collections.Generic;

namespace Resto.Front.Api.DataSaturation.Domain.Entities
{
    public class ProductInfoShortApi
    {
        public IList<ProductInfoShort> items { get; set; }
        public IList<ProductInfoShort> currentStopList { get; set; }

    }
}
