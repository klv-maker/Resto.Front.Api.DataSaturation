using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Screens;
using Resto.Front.Api.DataSaturation.Domain.Entities;
using Resto.Front.Api.DataSaturation.Domain.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using static Resto.Front.Api.DataSaturation.Helpers.JsonRPC;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class OrdersService : IOrdersService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private CancellationTokenSource cancellationSource;
        private bool isDisposed = false;
        private IOrder currentOrder;
        private OrderInfo currentOrderInfo;
        private object lockerCurrentOrder = new object();
        private object lockerCurrentOrderInfo = new object();
        private readonly IScreensService screensService;
        private bool enableOrdersServiceLocal;
        private string dataQRLocal;

        public OrdersService(IScreensService screensService, bool enableOrdersService, string dataQR)
        {
            cancellationSource = new CancellationTokenSource();
            subscriptions.Add(PluginContext.Notifications.OrderChanged.Subscribe(OrderChanged));
            this.screensService = screensService;
            this.screensService.OrderScreenOpened += OrderScreenOpened;
            enableOrdersServiceLocal = enableOrdersService;
            dataQRLocal = dataQR;
        }

        public void UpdateByCheckBox(bool enableOrdersService)
        {
            enableOrdersServiceLocal = enableOrdersService;
        }

        public void UpdateDataQR(string dataQR)
        {
            dataQRLocal = dataQR;
        }

        private void OrderScreenOpened(object sender, IOrder order)
        {
            if (enableOrdersServiceLocal)
            {
                if (isDisposed)
                    return;

                if (order is null)
                {
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderScreenOpened)}] Order is null");
                    currentOrder = null;
                    return;
                }

                //блокируем доступ до currentOrder 
                lock (lockerCurrentOrder)
                {

                    if (currentOrder?.Id == order.Id)
                    {
                        PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderScreenOpened)}] Current order same");
                        return;
                    }

                    currentOrder = order;
                }

                if (currentOrder.Items.Any())
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderScreenOpened)}] Order for sending {currentOrder}");
                    StartSendOrderInfo(currentOrder, EntityEventType.Created);
            }
        }

        private void OrderChanged(EntityChangedEventArgs<IOrder> obj)
        {
            if (enableOrdersServiceLocal)
            {
                if (isDisposed)
                    return;
                try
                {

                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] OrderChanged obj.Entity is null = {obj.Entity is null}");
                    if (obj.Entity is null)
                        return;

                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] Get order {obj.Entity.Id} {obj.Entity.Number}");
                    if (obj.Entity.Id != currentOrder?.Id)
                    {
                        PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] Order changed is not current order {currentOrder.Id} {currentOrder.Number}");
                        return;
                    }
                    lock (lockerCurrentOrder)
                    {
                        currentOrder = obj.Entity;
                    }

                    if (currentOrder.Items.Count == 1 && (obj.EventType == EntityEventType.Created || obj.EventType == EntityEventType.Updated) && !(currentOrder.Status == OrderStatus.Bill || currentOrder.Status == OrderStatus.Closed))
                    {
                        PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] Sending a new order {currentOrder}, {obj.EventType}");
                        StartSendOrderInfo(currentOrder, EntityEventType.Created);
                    }
                    else
                    {
                        PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] Order update {currentOrder}, {obj.EventType}");
                        StartSendOrderInfo(currentOrder, obj.EventType);
                    }
                } catch (Exception ex)
                {
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] {ex}");
                }
            }
        }

        private void StartSendOrderInfo(IOrder order, EntityEventType eventType)
        {
            PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] EnableOrdersServiceLocal = {enableOrdersServiceLocal}");
            if (enableOrdersServiceLocal)
            {
                if (isDisposed)
                    return;

                try
                {
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] Get order for send {order.Id} eventType = {eventType}");
                    var orderInfo = order.OrderToOrderInfo(eventType, dataQRLocal);
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] Get orderInfo = {orderInfo.SerializeToJson()}");
                    if (orderInfo is null || (orderInfo.orderStatus == OrderStatusInfo.start && order.Items.Count == 0))
                        return;

                    //блокируем чтобы не перебить
                    lock (lockerCurrentOrderInfo)
                    {
                        if (currentOrderInfo != null && currentOrderInfo.Equals(orderInfo))
                        {
                            PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] Old order info is equals with new - skip");
                            return;
                        }

                        currentOrderInfo = orderInfo;
                    }
                    Task.Run(async () => await Send(orderInfo), cancellationSource.Token);
                }
                catch (OperationCanceledException)
                {
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] Get task cancelled exception");
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Error($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] Get error when trying to send order {ex}");
                }
            }
        }

        private async Task Send(OrderInfo toSendData)
        {
            if (isDisposed)
                return;

            PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(Send)}] Start sending tasks");
            List<Task> tasks = new List<Task>();
            OrderInfoApi orderInfoApi = new OrderInfoApi()
            {
                data = toSendData,
                name = "iikoBasketStatus",
                type = ((toSendData.orderStatus == OrderStatusInfo.close) && (toSendData.visibleQR == true)) ? OrderStatusInfo.update.ToString() : (toSendData.orderStatus.ToString())
            };
            foreach (var address in Settings.Settings.Instance().AdressesApi)
            {
                tasks.Add(SendOrder(address, orderInfoApi));
            }
            await Task.WhenAll(tasks);
        }

        private async Task SendOrder(string url, OrderInfoApi toSendData)
        {
            if (isDisposed)
                return;

            try
            {
                using (var client = new JsonRpcClient(url))
                {
                    PluginContext.Log.Info(url);
                    var response = await client.SendRequestAsync(Constants.AddEvent, cancellationSource.Token, toSendData);
                    PluginContext.Log.Info(response);
                }
            }
            catch (TaskCanceledException ex)
            {
                if (ex.CancellationToken == cancellationSource.Token)
                    PluginContext.Log.Error($"[{nameof(OrdersService)}|{nameof(SendOrder)}] Get task cancelled exception");
                else
                {
                    PluginContext.Log.Error($"[{nameof(OrdersService)}|{nameof(SendOrder)}] Get task timeout exception");
                }
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(OrdersService)}|{nameof(SendOrder)}] Get task exception {ex}");
            }
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            if (screensService != null)
                screensService.OrderScreenOpened -= OrderScreenOpened;

            cancellationSource?.Cancel();
            cancellationSource?.Dispose();
            subscriptions?.Dispose();
        }
    }
}
