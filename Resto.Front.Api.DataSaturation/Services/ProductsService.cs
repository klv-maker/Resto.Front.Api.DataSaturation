using Newtonsoft.Json.Linq;
using Resto.Front.Api.Data.Assortment;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.UI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using static Resto.Front.Api.DataSaturation.Helpers.JsonRPC;
using System.Windows.Documents;


namespace Resto.Front.Api.DataSaturation.Services
{
    public class ProductsService : IProductsService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private CancellationTokenSource cancellationSource;
        private bool isDisposed = false;
        private int isStartedUpdateProducts = 0;
        private readonly ConcurrentDictionary<Guid, ProductInfo> stopList = new ConcurrentDictionary<Guid, ProductInfo>();
        private bool existsUpdateProductByTimeout = false;
        private Task initUpdateProductsTask = null;
        private Exception initUpdateException = null;
        public ProductsService() 
        {
            cancellationSource = new CancellationTokenSource();
            subscriptions.Add(PluginContext.Notifications.ProductChanged.Subscribe(ProductChanged));
            subscriptions.Add(PluginContext.Notifications.StopListProductsRemainingAmountsChanged.Subscribe(StopListChanged));
            subscriptions.Add(PluginContext.Operations.AddButtonToPluginsMenu("DataSaturationPlugin.Обмен", UpdateProducts));
            initUpdateProductsTask = StartUpdateProducts(false);
        }

        private Task StartUpdateProducts(bool needThrowException)
        {
            try
            {
                return Task.Run(async () =>
                {
                    try
                    {
                        if (Interlocked.CompareExchange(ref isStartedUpdateProducts, 1, 0) != 1)
                            await UpdateProducts();
                    }
                    catch (Exception ex)
                    {
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(StartUpdateProducts)}] Get error in update task: {ex}");
                        initUpdateException = ex;
                        if (needThrowException)
                            throw;
                    }
                }, cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(StartUpdateProducts)}] Get cancel");
                return null;
            }
            finally
            {
                Interlocked.Exchange(ref isStartedUpdateProducts, 0);
            }
        }

        public async Task UpdateProductByTimeout()
        {
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(UpdateProductByTimeout)}] Start UpdateProductByTimeout...");
            if (!existsUpdateProductByTimeout)
            {
                PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(UpdateProductByTimeout)}] existsUpdateProductByTimeout = false");
                existsUpdateProductByTimeout = true;
                try
                {
                    Task.Delay(3600000).Wait(cancellationSource.Token); //ждем с токеном отмены
                }
                catch (OperationCanceledException) { } //просто словили выход, не начинаем повторно отправку

                if (isDisposed || cancellationSource.IsCancellationRequested)
                    return;

                PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(UpdateProductByTimeout)}] Run UpdateProductByTimeout...");
                if (CheckFileFlag())
                {
                    try
                    {
                        await UpdateProducts();
                        existsUpdateProductByTimeout = false;
                    }
                    catch (Exception ex)
                    {
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(UpdateProductByTimeout)}] Get error: {ex}");
                        existsUpdateProductByTimeout = false;
                        UpdateProductByTimeout();
                    }
                }
            }
            return;
        }

        public void UpdateProducts((IViewManager vm, IReceiptPrinter printer) obj)
        {
            PluginContext.Log.Info($"[{nameof(ProductsService)}|static {nameof(UpdateProducts)}] Exchange was started by user {PluginContext.Operations.GetCurrentUser()?.Name}");
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            try
            {
                if (Interlocked.CompareExchange(ref isStartedUpdateProducts, 1, 0) == 1)
                {
                    if (initUpdateProductsTask != null)
                    {
                        initUpdateProductsTask.Wait(cancellationSource.Token);

                        if (initUpdateException.InnerException != null)
                        {
                            obj.vm.ShowErrorPopup($"Произошла ошибка обмена:\r\n {initUpdateException.InnerException.Message}");
                            return;
                        }
                        obj.vm.ShowErrorPopup($"Произошла ошибка обмена:\r\n {initUpdateException.Message}");
                        return;
                    }
                    //так как обмен уже был начат, просто выходим. защита от двойного нажатия (сомнительное воспроизведение)
                    return;
                }
                var userAnswer = obj.vm.ShowOkCancelPopup("Обмен", "Начать обмен?");
                if (!userAnswer)
                    return;

                StartUpdateProducts(true).Wait(cancellationSource.Token);
            }
            catch (OperationCanceledException)
            {
                PluginContext.Log.Info($"[{nameof(ProductsService)}|static {nameof(UpdateProducts)}] Get task cancelled exception");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|static {nameof(UpdateProducts)}] Get error when trying exchange: {ex}");
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
            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(StopListChanged)}] Get notification stop list changed");
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            Task.Run(async () => {
                try
                {
                    if (!CheckFileFlag())
                    {
                        await UpdateProductsByChangeStopList();
                    }
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(StopListChanged)}] Get error when trying to update stop list for products: {ex}");
                }
            }, cancellationSource.Token);
        }

        public async Task UpdateProductsByChangeStopList()
        {
            PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(UpdateProductsByChangeStopList)}] Start update stop list for products");
            if (isDisposed || cancellationSource.IsCancellationRequested)
                return;

            try
            {
                var stopListOld = new ConcurrentDictionary<Guid, ProductInfo>(stopList);
                GetStopLists();
                var toSendData = new ProductInfoShortApi();
                var productInfo = new List<ProductInfoShort>();

                foreach (var item in stopList)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopListOld.ContainsKey(item.Key))
                        continue;
                    var product = PluginContext.Operations.GetProductById(item.Value.id);
                    productInfo = product.GetProductInfoShort(item.Value);
                    toSendData.AddValuesToSendData(productInfo);
                }
                foreach (var item in stopListOld)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopList.ContainsKey(item.Key))
                        continue;

                    var product = PluginContext.Operations.GetProductById(item.Value.id);
                    productInfo = product.GetProductInfoShort();
                    toSendData.AddValuesToSendData(productInfo);
                }
                toSendData.currentStopList = stopList.GetProductInfoByStopList();
                PluginContext.Log.Info(toSendData.SerializeToJson());
                await Send(toSendData);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(UpdateProductsByChangeStopList)}] Get error when trying to update stop list: {ex}");
            }
        }

        private void ProductChanged(IProduct product)
        {
            try
            {
                PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Get update for product {product.Id} {product.Number} price = {product.Price}");
                Task.Run(async () =>
                {
                    try
                    {
                        if (!CheckFileFlag())
                        {
                            if (isDisposed || cancellationSource.IsCancellationRequested)
                                return;

                            stopList.TryGetValue(product.Id, out ProductInfo productInStopList);
                            var productInfo = product.GetProductInfoShort(productInStopList);
                            if (productInfo is null || !productInfo.Any())
                                return;

                            var toSendData = new ProductInfoShortApi();
                            toSendData.AddValuesToSendData(productInfo);
                            await Send(toSendData);
                            ModifiersService.Instance.UpdateProductModifierByPriceCategory(product);
                        }
                    }
                    catch (Exception ex)
                    {
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(StopListChanged)}] Get error when trying to send product {product.Id}: {ex}");
                    }
                }, cancellationSource.Token);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(ProductChanged)}] Get error when trying to update product {product.Id}: {ex}");
            }
        }

        private async Task UpdateProducts()
        {
            try
            {
                PluginContext.Log.Info($"[{nameof(ProductsService)}|{nameof(UpdateProducts)}] Start update products");
                if (isDisposed || cancellationSource.IsCancellationRequested)
                    return;

                GetStopLists();

                var products = PluginContext.Operations.GetActiveProducts().ToDictionary(product => product.Id, product => product);

                var toSendData = new ProductInfoShortApi();
                foreach (var product in products)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    stopList.TryGetValue(product.Key, out ProductInfo productInStopList);
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
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(UpdateProducts)}] Get error when trying to send products: {ex}");
                throw;
            }
        }

        public static bool CheckFileFlag()
        {
            var flagsPath = Path.Combine(PluginContext.Integration.GetDataStorageDirectoryPath(), "Flags");
            if (!Directory.Exists(flagsPath))
                return false;

            string filePath = Path.Combine(flagsPath, "flag.txt");
            return File.Exists(filePath);
        }

        public void CreateFileFlag()
        {
            if (!CheckFileFlag())
            {
                try
                {
                    var flagsPath = Path.Combine(PluginContext.Integration.GetDataStorageDirectoryPath(), "Flags");
                    if (!Directory.Exists(flagsPath))
                        Directory.CreateDirectory(flagsPath);

                    string filePath = Path.Combine(flagsPath, "flag.txt");
                    bool isFeatureEnabled = true;
                    File.WriteAllText(filePath, isFeatureEnabled.ToString());
                }
                catch
                {
                    PluginContext.Log.Error($"Получили ошибку при добавлении товара");
                }
            }
        }

        public void DeleteFileFlag()
        {
            if (CheckFileFlag())
            {
                try
                {
                    var flagsPath = Path.Combine(PluginContext.Integration.GetDataStorageDirectoryPath(), "Flags");
                    if (!Directory.Exists(flagsPath))
                        return;

                    string filePath = Path.Combine(flagsPath, "flag.txt");
                    File.Delete(filePath);
                }
                catch
                {
                    PluginContext.Log.Error($"Получили ошибку при удалении товара");
                }
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
                DeleteFileFlag();
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(Send)}] Get error when trying to send data: {ex}");
                CreateFileFlag();
                Task.Run(() => UpdateProductByTimeout());
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
                    var response = await client.SendRequestAsync(Constants.UpdateProducts, cancellationSource.Token, toSendData).ConfigureAwait(false);
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
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(SendProducts)}] Get task exception: {ex}");
                throw;
            }
        }

        public void GetStopLists()
        {
            try
            {
                var stopListDict = PluginContext.Operations.GetStopListProductsRemainingAmounts().ToDictionary(product => product.Key.Product.Id, product => product.Key);
                var currentStopList = stopList.Values.ToList();
                var stopListWithProductInfo = new Dictionary<Guid, ProductInfo>();
                foreach (var item in stopListDict)
                {
                    if (item.Value.ProductSize == null)
                    {
                        stopListWithProductInfo.Add(item.Key, item.Value.Product.GetProductInfo());
                    }
                    else
                    {
                        var fullProduct = item.Value.Product.GetProductInfo();
                        foreach (var size in fullProduct.productSize)
                        {
                            if (item.Value.ProductSize.Name != size.name)
                            {
                                fullProduct.productSize.Remove(size);
                            }
                        }
                        stopListWithProductInfo.Add(item.Key, fullProduct);
                    }
                }
                foreach (var item in currentStopList)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopListWithProductInfo.ContainsKey(item.id))
                        continue;

                    if (!stopList.TryRemove(item.id, out _))
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(GetStopLists)}] Get error when trying to delete product {item.id} from current stop list");
                }

                foreach (var item in stopListWithProductInfo.Values)
                {
                    if (cancellationSource.IsCancellationRequested)
                        return;

                    if (stopList.ContainsKey(item.id))
                    {
                        stopList[item.id] = item;
                        continue;
                    }

                    if (!stopList.TryAdd(item.id, item))
                        PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(GetStopLists)}] Get error when trying to add product {item.id} to current stop list");
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(GetStopLists)}] Get error when trying to update stop list {ex}");
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            try
            {
                isDisposed = true;
                cancellationSource?.Cancel();
                initUpdateProductsTask.Wait();
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(ProductsService)}|{nameof(Dispose)}] Get error when trying to dispose {ex}");
            }
            finally
            {
                cancellationSource?.Dispose();
                subscriptions?.Dispose();
            }
        }
    }
}
