using CommunityToolkit.Mvvm.Input;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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


        private ICommand removeCommand;
        public ICommand RemoveCommand
        {
            get
            {
                if (removeCommand == null)
                {
                    removeCommand = new RelayCommand(Remove);
                }
                return removeCommand;
            }
        }

        public Action CloseAction { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private Action<AddressViewModel> removeAction;
        public AddressViewModel(int number, string address, Action<AddressViewModel> removeAction)
        {
            this.AddressNumber = $"{number}";
            this.AddressApi = address;
            this.removeAction = removeAction;
        }

        public void UpdateIndex(int number)
        {
            this.AddressNumber = $"{number}";
        }

        public void Remove()
        {
            removeAction?.Invoke(this);
        }
    }
}
