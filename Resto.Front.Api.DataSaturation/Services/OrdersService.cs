using Resto.Front.Api.Data.Common;
using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Screens;
using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using static Resto.Front.Api.DataSaturation.Helpers.JsonRPC;

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

        public OrdersService()
        {
            cancellationSource = new CancellationTokenSource();
            subscriptions.Add(PluginContext.Notifications.ScreenChanged.Subscribe(ScreenChanged));
            subscriptions.Add(PluginContext.Notifications.OrderChanged.Subscribe(OrderChanged));
        }

        private void ScreenChanged(IScreen screen)
        {
            if (isDisposed)
                return;

            PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(ScreenChanged)}] Changed screen");
            if (!(screen is IOrderEditScreen orderEditScreen))
            {
                PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(ScreenChanged)}] Changed screen not order screen");
                return; 
            }

            //блокируем доступ до currentOrder 
            lock (lockerCurrentOrder)
            {
                if (orderEditScreen.Order is null)
                {
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(ScreenChanged)}] Order is null");
                    currentOrder = null;
                    return;
                }

                if (currentOrder?.Id == orderEditScreen.Order.Id)
                {
                    PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(ScreenChanged)}] Current order same");
                    return;
                }

                currentOrder = orderEditScreen.Order;
            }

            StartSendOrderInfo(currentOrder, EntityEventType.Created);
        }

        private void OrderChanged(EntityChangedEventArgs<IOrder> obj)
        {
            if (isDisposed)
                return;

            PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] OrderChanged obj.Entity is null = {obj.Entity is null}");
            if (obj.Entity is null)
                return;

            PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] Get order {obj.Entity.Id} {obj.Entity.Number}");
            if (obj.Entity.Id != currentOrder.Id)
            {
                PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(OrderChanged)}] Order changed is not current order {currentOrder.Id} {currentOrder.Number}");
                return;
            }
            lock (lockerCurrentOrder) 
            {
                currentOrder = obj.Entity;
            }
            StartSendOrderInfo(currentOrder, obj.EventType);
        }

        private void StartSendOrderInfo(IOrder order, EntityEventType eventType)
        {
            if (isDisposed)
                return;

            try
            {
                PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] Get order for send {order.Id} eventType = {eventType}");
                var orderInfo = order.OrderToOrderInfo(eventType);
                PluginContext.Log.Info($"[{nameof(OrdersService)}|{nameof(StartSendOrderInfo)}] Get orderInfo = {orderInfo.SerializeToJson()}");
                if (orderInfo is null)
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
                type = toSendData.orderStatus.ToString()
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
                var client = new JsonRpcClient(url);
                PluginContext.Log.Info(url);

                var response = await client.SendRequestAsync("addEvent", cancellationSource.Token, new object[] { toSendData });
                PluginContext.Log.Info(response);
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
            cancellationSource?.Cancel();
            cancellationSource?.Dispose();
            subscriptions?.Dispose();
        }
    }
}
