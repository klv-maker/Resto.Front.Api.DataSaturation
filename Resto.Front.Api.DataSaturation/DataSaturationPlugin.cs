using Resto.Front.Api.Attributes;
using Resto.Front.Api.Attributes.JetBrains;
using Resto.Front.Api.DataSaturation.Interfaces;
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

        public DataSaturationPlugin()
        {
            productsService = new ProductsService();
            settingsService = new SettingsService();
        }

        public void Dispose()
        {
            productsService.Dispose();
            settingsService.Dispose();
        }
    }
}
