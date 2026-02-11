using Resto.Front.Api.DataSaturation.Domain.Views;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces.Services;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces.VIewModels;
using Resto.Front.Api.DataSaturation.MindBox.ViewModels;
using Resto.Front.Api.DataSaturation.MindBox.Views;
using Resto.Front.Api.DataSaturation.Settings;
using Resto.Front.Api.UI;
using System.Reactive.Disposables;

namespace Resto.Front.Api.DataSaturation.MindBox.Services
{
    public class MindBoxSettingsService : IMindBoxSettingsService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        private WindowOwner windowOwner;
        private IMindBoxSettingsViewModel settingsViewModel;
        private bool isDisposed = false;
        public MindBoxSettingsService()
        {
            subscriptions.Add(PluginContext.Operations.AddButtonToPluginsMenu("DataSaturationPlugin.MindBox", ShowSettingsMindbox));
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

        private void ShowSettingsMindbox((IViewManager vm, IReceiptPrinter printer) tuple)
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
            settingsViewModel = new MindBoxSettingsViewModel(MindBoxSettings.Instance());
            windowOwner.ShowDialog<MindBoxSettingsWindow>(settingsViewModel);
        }
    }
}
