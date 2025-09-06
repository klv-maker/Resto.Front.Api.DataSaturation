using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class ProductsService : IProductsService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private ConcurrentDictionary<Guid, ProductInfo> products = new ConcurrentDictionary<Guid, ProductInfo>();
        private object locker = new object();
        public ProductsService() 
        {
            subscriptions.Add(PluginContext.Notifications.ProductChanged.Subscribe(ProductChanged));
        }

        public void Dispose()
        {
            subscriptions?.Dispose();
        }

        private void ProductChanged(IProduct product)
        {
            //TODO: баркод в офисе был пустой, возможно нужно будет поменять на него или сменить название параметра в ProductInfo
            var code = product.Number;
            if (string.IsNullOrWhiteSpace(code))
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Get changed product id = {product.Id} with empty number!");
                return; 
            }

            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Get changed product id = {product.Id} number = {code} price = {product.Price}");
            var dataToSend = new ProductInfo{ barcode = code, price = product.Price };
            //блокируем доступ, так как может прийти обновление продукта, а мы получаем данные
            lock (locker)
                products.AddOrUpdate(product.Id, dataToSend, (id, data) => dataToSend );
        }

        public string GetProductsChangedInJson()
        {
            //пока не получили все данные не даем возможность добавлять данные в коллекцию
            lock (locker)
            {
                var toReturnData = new List<ProductInfo>();
                foreach (var product in products)
                {
                    toReturnData.Add(product.Value);
                }
                products.Clear();
                return toReturnData.SerializeToJson();
            }
        }
    }
}
