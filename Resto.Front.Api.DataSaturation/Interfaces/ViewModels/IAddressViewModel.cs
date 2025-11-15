using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.Interfaces.ViewModels
{
    public interface IAddressViewModel : IViewModel
    {
        string AddressApi { get; }
        ICommand RemoveCommand { get; }
        void UpdateIndex(int number);
    }
}
