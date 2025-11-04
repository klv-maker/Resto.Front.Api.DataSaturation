using Resto.Front.Api.DataSaturation.Interfaces;
using Resto.Front.Api.DataSaturation.ViewModels;
using Resto.Front.Api.DataSaturation.Views;
using Resto.Front.Api.UI;
using System.Reactive.Disposables;

namespace Resto.Front.Api.DataSaturation.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly CompositeDisposable subscriptions = new CompositeDisposable();
        public SettingsService()
        {
            subscriptions.Add(PluginContext.Operations.AddButtonToPluginsMenu("DataSaturationPlugin.Settings", ShowSettingsPlugin));
        }

        public void Dispose()
        {
            subscriptions?.Dispose();
        }

        public void ShowSettingsPlugin((IViewManager viewManager, IReceiptPrinter receiptPrinter) obj)
        {
            using (var windowOwner = new WindowOwner())
            {
                var viewModel = new SettingsViewModel(Settings.Settings.Instance().AdressesApi);
                windowOwner.ShowDialog<SettingsWindow>(viewModel);
            }
        }

    }
}
