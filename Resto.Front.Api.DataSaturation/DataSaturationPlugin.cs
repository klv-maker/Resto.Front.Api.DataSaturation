using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.Services;

namespace Resto.Front.Api.DataSaturation
{
    [UsedImplicitly, PluginLicenseModuleId(ModuleId)]
    public class DataSaturationPlugin : IFrontPlugin
    {
        //ид модуля так-то для плагина сторонней оплаты, но это мы просто нацелены на дальнейшую интеграцию с iikoCard
        private const int ModuleId = 21016318;
        private readonly IProductsService productsService;
        private readonly ISettingsService settingsService;
        private readonly IOrdersService ordersService;
        private readonly IScreensService screensService;
        private readonly ILockService lockScreenService;
        private readonly IBarcodeScannerService barcodeScannerService;

        public DataSaturationPlugin()
        {
            screensService = new ScreensService();
            productsService = new ProductsService();
            lockScreenService = new LockScreenService(screensService);
            lockScreenService.UpdateSwitchMediaTime(Settings.Settings.Instance().SwitchMediaTime);
            ordersService = new OrdersService(screensService, Settings.Settings.Instance().EnableOrdersService);
            settingsService = new SettingsService(lockScreenService, ordersService);
            barcodeScannerService = new BarcodeScannerService();
        }

        public void Dispose()
        {
            productsService.Dispose();
            settingsService.Dispose();
            ordersService.Dispose();
            screensService.Dispose();
            lockScreenService.Dispose();
            ModifiersService.Instance.Dispose();
        }
    }
}
