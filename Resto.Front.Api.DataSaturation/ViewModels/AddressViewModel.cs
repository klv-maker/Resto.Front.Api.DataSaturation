using Resto.Front.Api.DataSaturation.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Resto.Front.Api.DataSaturation.ViewModels
{
    public class AddressViewModel : IAddressViewModel
    {
        private string addressNumber;
        public string AddressNumber
        {
            get
            {
                return addressNumber; 
            }
            set 
            {
                addressNumber = value; 
                OnPropertyChanged(nameof(AddressNumber));
            }
        }
        private string addressApi;
        public string AddressApi
        {
            get 
            { 
                return addressApi;
            }
            set 
            {
                addressApi = value;
                OnPropertyChanged(nameof(AddressApi));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public AddressViewModel(int number, string address)
        {
            this.AddressNumber = $"{number}";
            this.AddressApi = address;
        }
    }
}
