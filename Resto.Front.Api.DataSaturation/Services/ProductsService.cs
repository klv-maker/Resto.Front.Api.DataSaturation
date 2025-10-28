using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using Resto.Front.Api.DataSaturation.Settings;
using Resto.Front.Api.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Resto.Front.Api.DataSaturation.Helpers.JsonRPC;


namespace Resto.Front.Api.DataSaturation.Services
{
    public class ProductsService : IProductsService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private CancellationTokenSource cancellationSource;
        private bool isDisposed = false;
        private int isStartedUpdateProducts = 0;
        private ConcurrentDictionary<Guid, ProductAndSize> stopList = new ConcurrentDictionary<Guid, ProductAndSize>();
        public ProductsService() 
        {
            cancellationSource = new CancellationTokenSource();
            subscriptions.Add(PluginContext.Notifications.ProductChanged.Subscribe(ProductChanged));
            subscriptions.Add(PluginContext.Notifications.StopListProductsRemainingAmountsChanged.Subscribe(OnChangeStopList));
            subscriptions.Add(PluginContext.Operations.AddButtonToPluginsMenu("DataSaturationPlugin.Обмен", UpdateProducts));
        }

        public void UpdateProducts((IViewManager vm, IReceiptPrinter printer) obj)
        {
            try
            {
                if (Interlocked.CompareExchange(ref isStartedUpdateProducts, 1, 0) == 1)
                {
                    obj.vm.ShowOkPopup("Обмен", "Обмен уже был начат");
                    return;
                }
                Task.Run(async () => await UpdateProducts(), cancellationSource.Token);
                obj.vm.ShowOkPopup("Обмен", "Обмен начат");
            }
            catch (Exception ex)
            {
                obj.vm.ShowOkPopup("Обмен", $"{ex}");
            }
        }

        public void OnChangeStopList(VoidValue voidValue)
        {
            PluginContext.Log.Info("Вызван метод OnChangeStopList...");
            UpdateProductsByChangeStopList();
        }

        async public Task UpdateProductsByChangeStopList()
        {
            var stopListOld = stopList;
            GetStopLists();
            var toSendData = new ProductInfoShortApi();
            var productInfo = new List<ProductInfoShort>();

            foreach (var item in stopList)
            {
                if (!stopListOld.ContainsKey(item.Key))
                {
                    productInfo = item.Value.Product.GetProductInfoShort(item.Value);
                    toSendData.AddValuesToSendData(productInfo);
                }
            }
            foreach (var item in stopListOld)
            {
                if (!stopList.ContainsKey(item.Key))
                {
                    productInfo = item.Value.Product.GetProductInfoShort();
                    toSendData.AddValuesToSendData(productInfo);
                }
            }
            toSendData.currentStopList = stopList.GetProductInfoByStopList();
            PluginContext.Log.Info(toSendData.SerializeToJson());
            await Send(toSendData);
        }
       
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            cancellationSource?.Cancel();
            cancellationSource?.Dispose();
            subscriptions?.Dispose();
        }

        private void ProductChanged(IProduct product)
        {
            if (isDisposed)
                return;
            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Get changed product id = {product.Id} {product.Number} price = {product.Price}");

            var productInfo = product.GetProductInfoShort();
            if (productInfo is null || !productInfo.Any())
                return;

            var toSendData = new ProductInfoShortApi();
            toSendData.AddValuesToSendData(productInfo);

            Task.Run(async () => await Send(toSendData), cancellationSource.Token);
        }

        private async Task UpdateProducts()
        {
            if (isDisposed)
                return;

            stopList = GetStopLists();
            var products = PluginContext.Operations.GetActiveProducts();
            Dictionary<string, IProduct> productsDict = new Dictionary<string, IProduct>();
            foreach (var product in products)
            {
                if (cancellationSource.IsCancellationRequested) 
                    return;

                if (productsDict.ContainsKey(product.Number))
                    continue;

                //if (product.Price <= 0)
                    //continue;

                productsDict.Add(product.Number, product);
            }
            var toSendData = new ProductInfoShortApi();
            toSendData.items = new List<ProductInfoShort>();
            foreach (var product in productsDict)
            {
                if (cancellationSource.IsCancellationRequested)
                    return;
                var productInStopList = stopList.FirstOrDefault(_ => _.Key == product.Value.Id).Value;
                var productInfo = product.Value.GetProductInfoShort(productInStopList);
                if (productInfo is null && !productInfo.Any())
                    continue;

                toSendData.AddValuesToSendData(productInfo);
            }
            toSendData.currentStopList = stopList.GetProductInfoByStopList();
            await Send(toSendData);
        }

        private async Task Send(ProductInfoShortApi toSendData)
        {
            if (isDisposed)
                return;

            List<Task> tasks = new List<Task>();
            foreach (var address in Settings.Settings.Instance().AdressesApi)
            {
                tasks.Add(SendProducts(address, toSendData));
            }
            await Task.WhenAll(tasks);
        }

        private async Task SendProducts(string url, ProductInfoShortApi toSendData)
        {
            if (isDisposed)
                return;

            try
            {
                var client = new JsonRpcClient(url);
                PluginContext.Log.Info(url);
                var response = await client.SendRequestAsync("updateProducts", cancellationSource.Token, new object[] { toSendData });
                PluginContext.Log.Info(response);
                Interlocked.Exchange(ref isStartedUpdateProducts, 0);
            }
            catch (TaskCanceledException ex) 
            {
                if (ex.CancellationToken == cancellationSource.Token)
                    PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(SendProducts)}] Get task cancelled exception");
                else
                {
                    PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(SendProducts)}] Get task timeout exception");
                }
                Interlocked.Exchange(ref isStartedUpdateProducts, 0);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(SendProducts)}] Get task exception {ex}");
                Interlocked.Exchange(ref isStartedUpdateProducts, 0);
            }
        }


        public ConcurrentDictionary<Guid, ProductAndSize> GetStopLists()
        {
            var stopListDict = PluginContext.Operations.GetStopListProductsRemainingAmounts();
            stopList = new ConcurrentDictionary<Guid, ProductAndSize>();
            foreach (var item in stopListDict)
            {
                PluginContext.Log.Info(item.Key.SerializeToJson());
                stopList.TryAdd(item.Key.Product.Id, item.Key);
            }
            return stopList;
        }
    }
}
