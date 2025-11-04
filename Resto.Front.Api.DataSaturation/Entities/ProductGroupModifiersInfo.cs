using Resto.Front.Api.Data.Assortment;
using System;
using System.Collections.Concurrent;

namespace Resto.Front.Api.DataSaturation.Entities
{
    public class ProductGroupModifiersInfo
    {
        public IProduct Product { get; set; }
        public ConcurrentDictionary<Guid, IGroupModifier> GroupModifiers { get; set; } = new ConcurrentDictionary<Guid, IGroupModifier>();
        public ProductGroupModifiersInfo(IProduct product)
        {
            Product = product;
            GroupModifiers = new ConcurrentDictionary<Guid, IGroupModifier>();
        }
    }
}
