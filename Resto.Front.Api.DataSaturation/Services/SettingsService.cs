using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using Resto.Front.Api.DataSaturation.ViewModels;
using Resto.Front.Api.DataSaturation.Views;
using Resto.Front.Api.UI;
using System.Reactive.Disposables;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private WindowOwner windowOwner;
        private ISettingsViewModel settingsViewModel;
        private bool isDisposed = false;
        private ILockService lockService;
        private IOrdersService orderService;
        public SettingsService(ILockService lockService, IOrdersService orderService)
        {
            this.lockService = lockService;
            this.orderService = orderService;
            subscriptions.Add(PluginContext.Operations.AddButtonToPluginsMenu("DataSaturationPlugin.Settings", ShowSettingsPlugin));
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            subscriptions?.Dispose();
            if (settingsViewModel?.CloseAction != null)
                settingsViewModel.CloseAction();
            windowOwner?.Dispose();
        }

        public void ShowSettingsPlugin((IViewManager viewManager, IReceiptPrinter receiptPrinter) obj)
        {
            if (isDisposed)
                return;

            //явно вызываем очистку
            if (windowOwner != null)
            {
                windowOwner.Dispose();
                windowOwner = null;
            }
            windowOwner = new WindowOwner();            
            settingsViewModel = new SettingsViewModel(lockService, orderService, Settings.Settings.Instance());
            windowOwner.ShowDialog<SettingsWindow>(settingsViewModel);
        }

    }
}
