using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
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
        private readonly ConcurrentDictionary<Guid, ProductAndSize> stopList = new ConcurrentDictionary<Guid, ProductAndSize>();
        public ProductsService() 
        {
            cancellationSource = new CancellationTokenSource();
            subscriptions.Add(PluginContext.Notifications.ProductChanged.Subscribe(ProductChanged));
            subscriptions.Add(PluginContext.Notifications.StopListProductsRemainingAmountsChanged.Subscribe(StopListChanged));
            subscriptions.Add(PluginContext.Operations.AddButtonToPluginsMenu("DataSaturationPlugin.Обмен", UpdateProducts));
        }

        public void UpdateProducts((IViewManager vm, IReceiptPrinter printer) obj)
        {
            PluginContext.Log.Info($"[{nameof(ProductsService)}|static {nameof(UpdateProducts)}] Запуск обмена пользователем {PluginContext.Operations.GetCurrentUser()?.Name}");
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            try
            {
                if (Interlocked.CompareExchange(ref isStartedUpdateProducts, 1, 0) == 1)
                {
                    obj.vm.ShowOkPopup("Обмен", "Обмен уже был начат");
                    return;
                }
                var userAnswer = obj.vm.ShowOkCancelPopup("Обмен", "Начать обмен?");
                if (!userAnswer)
                    return;

                UpdateProducts().Wait(cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                PluginContext.Log.Info($"[{nameof(ProductsService)}|static {nameof(UpdateProducts)}] Get task cancelled exception");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|static {nameof(UpdateProducts)}] Ошибка при обмене: {ex}");
                if (ex.InnerException != null)
                {
                    obj.vm.ShowErrorPopup($"Произошла ошибка обмена:\r\n {ex.InnerException.Message}");
                    return;
                }
                obj.vm.ShowErrorPopup($"Произошла ошибка обмена:\r\n {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref isStartedUpdateProducts, 0);
            }
        }

        public void StopListChanged(VoidValue voidValue)
        {
            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(StopListChanged)}] Вызван метод StopListChanged...");
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            Task.Run(async () => {
                try
                {
                    await UpdateProductsByChangeStopList();
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(StopListChanged)}] Получили ошибку: {ex}");
                }
            }, cancellationSource.Token);
        }

        public async Task UpdateProductsByChangeStopList()
        {
            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(StopListChanged)}] Вызван метод UpdateProductsByChangeStopList...");
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            try
            {
                var stopListOld = new ConcurrentDictionary<Guid, ProductAndSize>(stopList);
                GetStopLists();
                var toSendData = new ProductInfoShortApi();
                var productInfo = new List<ProductInfoShort>();

                foreach (var item in stopList)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopListOld.ContainsKey(item.Key))
                        continue;

                    productInfo = item.Value.Product.GetProductInfoShort(item.Value);
                    toSendData.AddValuesToSendData(productInfo);
                }
                foreach (var item in stopListOld)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopList.ContainsKey(item.Key))
                        continue;

                    productInfo = item.Value.Product.GetProductInfoShort();
                    toSendData.AddValuesToSendData(productInfo);
                }
                toSendData.currentStopList = stopList.GetProductInfoByStopList();
                PluginContext.Log.Info(toSendData.SerializeToJson());
                await Send(toSendData);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(UpdateProductsByChangeStopList)}] Получили ошибку при обновлении стоплиста {ex}");
            }
        }

        private void ProductChanged(IProduct product)
        {
            try
            {
                PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Получили обновление продукта {product.Id} {product.Number} price = {product.Price}");
                Task.Run(async () =>
                {
                    try
                    {
                        if (isDisposed || cancellationSource.IsCancellationRequested)
                            return;

                        stopList.TryGetValue(product.Id, out ProductAndSize productInStopList);
                        var productInfo = product.GetProductInfoShort(productInStopList);
                        if (productInfo is null || !productInfo.Any())
                            return;

                        var toSendData = new ProductInfoShortApi();
                        toSendData.AddValuesToSendData(productInfo);
                        await Send(toSendData);
                        ModifiersService.Instance.UpdateProductModifierByPriceCategory(product);
                    }
                    catch (Exception ex)
                    {
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(StopListChanged)}] Получили ошибку отправки изменения продукта {product.Id}: {ex}");
                    }
                }, cancellationSource.Token);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Получили ошибку при обновлении продукта {product.Id}\r\n {ex}");
            }
        }

        private async Task UpdateProducts()
        {
            try
            {
                if (isDisposed || cancellationSource.IsCancellationRequested)
                    return;

                GetStopLists();

                var products = PluginContext.Operations.GetActiveProducts().ToDictionary(product => product.Id, product => product);

                var toSendData = new ProductInfoShortApi();
                foreach (var product in products)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    stopList.TryGetValue(product.Key, out ProductAndSize productInStopList);
                    var productInfo = product.Value.GetProductInfoShort(productInStopList);
                    if (productInfo is null || !productInfo.Any())
                        continue;

                    toSendData.AddValuesToSendData(productInfo);
                }
                toSendData.currentStopList = stopList.GetProductInfoByStopList();
                await Send(toSendData);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(UpdateProducts)}] Получили ошибку при отправке {ex}");
                throw;
            }
        }

        private async Task Send(ProductInfoShortApi toSendData)
        {
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            try
            {
                List<Task> tasks = new List<Task>();
                foreach (var address in Settings.Settings.Instance().AdressesApi)
                {
                    tasks.Add(SendProducts(address, toSendData));
                }
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(Send)}] Ошибка при отправке данных: {ex}");
                throw;
            }
        }

        private async Task SendProducts(string url, ProductInfoShortApi toSendData)
        {
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            try
            {
                using (var client = new JsonRpcClient(url))
                {
                    PluginContext.Log.Info(url);
                    var response = await client.SendRequestAsync("updateProducts", cancellationSource.Token, new object[] { toSendData }).ConfigureAwait(false);
                    PluginContext.Log.Info(response);
                }
            }
            catch (TaskCanceledException ex) 
            {
                if (ex.CancellationToken == cancellationSource.Token)
                    PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(SendProducts)}] Get task cancelled exception");
                else
                    PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(SendProducts)}] Get task timeout exception");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(SendProducts)}] Get task exception {ex}");
                throw;
            }
        }

        public void GetStopLists()
        {
            try
            {
                var stopListDict = PluginContext.Operations.GetStopListProductsRemainingAmounts().ToDictionary(product => product.Key.Product.Id, product => product.Key);
                var currentStopList = stopList.Values.ToList();
                foreach (var item in currentStopList)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopListDict.ContainsKey(item.Product.Id))
                        continue;

                    if (!stopList.TryRemove(item.Product.Id, out _))
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(GetStopLists)}] Ошибка удаления продукта из списка текущих стоплистов {item.Product.Id}");
                }

                foreach (var item in stopListDict.Values)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopList.ContainsKey(item.Product.Id))
                    {
                        stopList[item.Product.Id] = item;
                        continue;
                    }

                    if (!stopList.TryAdd(item.Product.Id, item))
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(GetStopLists)}] Ошибка добавления продукта в список текущих стоплистов {item.Product.Id}");
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(GetStopLists)}] Получили ошибку при обновлении стоп листа {ex}");
            }
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
    }
}
