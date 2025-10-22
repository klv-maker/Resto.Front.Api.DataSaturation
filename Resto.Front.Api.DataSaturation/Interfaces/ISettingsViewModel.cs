using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.Interfaces
{
    public interface ISettingsViewModel : IViewModel
    {
        ObservableCollection<IAddressViewModel> AddressViewModels { get; }
        Action CloseAction { get; set; }
        ICommand CancelCommand { get; }
        ICommand SaveCommand { get; }
        ICommand AddCommand { get; }
        ICommand RemoveCommand { get; }
        IAddressViewModel SelectedAddress { get; set; }
    }
}
