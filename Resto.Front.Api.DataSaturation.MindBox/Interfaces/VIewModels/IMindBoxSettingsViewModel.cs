using Resto.Front.Api.DataSaturation.Domain.Interfaces.ViewModels;

namespace Resto.Front.Api.DataSaturation.MindBox.Interfaces.VIewModels
{
    public interface IMindBoxSettingsViewModel : IClosebaleViewModel
    {
        string AddressApi { get; set; }
        string Key { get; set; }
    }
}
