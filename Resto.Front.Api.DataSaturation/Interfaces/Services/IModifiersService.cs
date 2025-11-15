using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.Data.Orders;
using System;

namespace Resto.Front.Api.DataSaturation.Interfaces.Services
{
    public interface IModifiersService : IDisposable
    {
        IChildModifier GetGroupModifierInfo(IProduct product, Guid modifierId);
        void UpdateProductModifierByPriceCategory(IProduct product);
    }
}
