using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.Interfaces.ViewModels
{
    public interface ISettingsViewModel : IViewModel
    {
        ObservableCollection<IAddressViewModel> AddressViewModels { get; }
        int SwitchMediaTime { get; set; }
        ICommand CancelCommand { get; }
        ICommand SaveCommand { get; }
        ICommand AddCommand { get; }
        IAddressViewModel SelectedAddress { get; set; }
        void Update(ISettings settings);
    }
}
