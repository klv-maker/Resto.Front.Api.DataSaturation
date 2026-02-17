using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces.Services;
using Resto.Front.Api.DataSaturation.MindBox.Services;
using Resto.Front.Api.DataSaturation.Services;
using Resto.Front.Api.DataSaturation.Settings;
using System;

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
        private readonly IMindBoxService mindBoxService;
        private readonly IMindBoxSettingsService mindBoxSettingsService;
        private readonly IIikoCardService iikoCardService;

        public DataSaturationPlugin()
        {
            bool is64Bit = Environment.Is64BitProcess;
            lockScreenService = new LockScreenService();
            screensService = new ScreensService();
            screensService.LockScreenChanged += lockScreenService.LockScreenChanged;
            productsService = new ProductsService();
            ordersService = new OrdersService(screensService, Settings.Settings.Instance().EnableOrdersService, Settings.Settings.Instance().DataQR);
            iikoCardService = new IikoCardService(Settings.Settings.Instance().IikoCard);
            settingsService = new SettingsService(ordersService, iikoCardService);
            barcodeScannerService = new BarcodeScannerService(iikoCardService);
            mindBoxSettingsService = new MindBoxSettingsService();
            mindBoxService = new MindBoxService(MindBoxSettings.Instance());
        }

        public void Dispose()
        {
            productsService.Dispose();
            settingsService.Dispose();
            ordersService.Dispose();
            screensService.LockScreenChanged -= lockScreenService.LockScreenChanged;
            screensService.Dispose();
            lockScreenService.Dispose();
            ModifiersService.Instance.Dispose();
            mindBoxSettingsService.Dispose();
            mindBoxService.Dispose();
            iikoCardService.Dispose();
            barcodeScannerService.Dispose();
        }
    }
}
