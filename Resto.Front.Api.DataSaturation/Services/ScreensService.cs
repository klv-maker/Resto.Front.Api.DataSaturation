using Resto.Front.Api.Data.Orders;
using Resto.Front.Api.Data.Screens;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using System;
using System.Reactive.Disposables;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class ScreensService : IScreensService
    {
        public EventHandler<IOrder> OrderScreenOpened { get; set; }
        public EventHandler<bool> LockScreenChanged { get; set; }
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private bool isDisposed = false;
        private bool isLockScreenOpened = false;
        public ScreensService() 
        {
            subscriptions.Add(PluginContext.Notifications.ScreenChanged.Subscribe(ScreenChanged));
        }

        private void ScreenChanged(IScreen screen)
        {
            PluginContext.Log.Info($"[{nameof(ScreensService)}|{nameof(ScreenChanged)}] Changed screen");
            if (!(screen is ILockScreen) && isLockScreenOpened)
            {
                PluginContext.Log.Info($"[{nameof(ScreensService)}|{nameof(ScreenChanged)}] Send close lock screen"); 
                SendLockScreenChanged(false);
            }

            if (screen is IOrderEditScreen orderEditScreen)
            {
                PluginContext.Log.Info($"[{nameof(ScreensService)}|{nameof(ScreenChanged)}] Is screen order");
                OrderScreenOpened?.Invoke(this, orderEditScreen.Order);
                return;
            }
            if (screen is ILockScreen lockScreen)
            {
                PluginContext.Log.Info($"[{nameof(ScreensService)}|{nameof(ScreenChanged)}] Is lock screen");
                SendLockScreenChanged(true);
                return;
            }
        }

        public void SendLockScreenChanged(bool isOpen)
        {
            isLockScreenOpened = isOpen;
            LockScreenChanged?.Invoke(this, isOpen);
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            subscriptions?.Dispose();
            OrderScreenOpened = null;
            LockScreenChanged = null;
        }
    }
}
