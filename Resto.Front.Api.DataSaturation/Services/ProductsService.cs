using Resto.Front.Api.Data.Assortment;
using System;
using System.Linq;
using System.Reactive.Disposables;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class ProductsService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        public ProductsService() 
        {
            subscriptions.Add(PluginContext.Notifications.ProductChanged.Subscribe(ProductChanged));
        }

        private void ProductChanged(IProduct product)
        {
            var barcode = product.BarcodeContainers.FirstOrDefault();
            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Get changed product id = {product.Id} code = {barcode} price = {product.Price}");

        }
    }
}
