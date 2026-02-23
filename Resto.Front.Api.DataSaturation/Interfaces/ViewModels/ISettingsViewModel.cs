using Resto.Front.Api.DataSaturation.Domain.Interfaces.ViewModels;
using Resto.Front.Api.DataSaturation.Settings;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.Interfaces.ViewModels
{
    public interface ISettingsViewModel : IClosebaleViewModel
    {
        ObservableCollection<IAddressViewModel> AddressViewModels { get; }
        int SwitchMediaTime { get; set; }
        bool EnableOrdersService { get; set; }
        string DataQR { get; set; }
        IikoCard IikoCard { get; set; }
        ICommand CancelCommand { get; }
        ICommand SaveCommand { get; }
        ICommand AddCommand { get; }
        IAddressViewModel SelectedAddress { get; set; }
    }
}
