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
        public SettingsService(ILockService lockService)
        {
            this.lockService = lockService;
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
        }

        public void ShowSettingsPlugin((IViewManager viewManager, IReceiptPrinter receiptPrinter) obj)
        {
            if (isDisposed)
                return;

            windowOwner = new WindowOwner();
            if (settingsViewModel is null)
                settingsViewModel = new SettingsViewModel(lockService);

            settingsViewModel.Update(Settings.Settings.Instance());
            windowOwner.ShowDialog<SettingsWindow>(settingsViewModel);
        }

    }
}
