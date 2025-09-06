using System;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface IProductsService : IDisposable
    {
        /// <summary>
        /// получаем список отредактированных товаров в json
        /// </summary>
        /// <returns>json массива отредактированных товаров в ProductInfo</returns>
        string GetProductsChangedInJson();
    }
}
