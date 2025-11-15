using CommunityToolkit.Mvvm.Input;
using Resto.Front.Api.DataSaturation.Interfaces;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.ViewModels
{
    public class SettingsViewModel : ISettingsViewModel
    {
        private ObservableCollection<IAddressViewModel> addressViewModels = new ObservableCollection<IAddressViewModel>();
        public ObservableCollection<IAddressViewModel> AddressViewModels
        {
            get 
            { 
                return addressViewModels;
            }
            set
            {
                addressViewModels = value;
            }
        }

        private int switchMediaTime;
        public int SwitchMediaTime
        {
            get
            {
                return switchMediaTime;
            }
            set
            {
                switchMediaTime = value;
                OnPropertyChanged(nameof(SwitchMediaTime));
            }
        }
        public Action CloseAction { get; set; }

        private ICommand cancelCommand;
        public ICommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(Cancel);
                }
                return cancelCommand;
            }
        }

        private ICommand saveCommand;
        public ICommand SaveCommand
        {
            get
            {
                if (saveCommand == null)
                {
                    saveCommand = new RelayCommand(Save);
                }
                return saveCommand;
            }
        }

        private ICommand addCommand;
        public ICommand AddCommand
        {
            get
            {
                if (addCommand == null)
                {
                    addCommand = new RelayCommand(Add);
                }
                return addCommand;
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

        private IAddressViewModel selectedAddress;
        public IAddressViewModel SelectedAddress
        {
            get { return selectedAddress; }
            set
            {
                selectedAddress = value;
                OnPropertyChanged(nameof(SelectedAddress));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private ILockService lockService;

        public SettingsViewModel(ILockService lockService) 
        {
            this.lockService = lockService;
        }

        public void Update(ISettings settings)
        {
            if (settings is null)
                AddressViewModels.Clear();
            if (settings.AdressesApi is null)
                AddressViewModels.Clear();

            for (var i = 0; i < settings.AdressesApi.Count; i++)
            {
                PluginContext.Log.Info($"Trying to add address: {settings.AdressesApi[i]}");
                AddressViewModels.Add(new AddressViewModel(i + 1, settings.AdressesApi[i]));
            }
            SwitchMediaTime = settings.SwitchMediaTime;
        }


        public void Cancel()
        {
            CloseAction?.Invoke();
        }

        public void Save()
        {
            try
            {
                List<string> addresses = new List<string>();
                foreach (var address in AddressViewModels)
                {
                    addresses.Add(address.AddressApi);
                }
                Settings.Settings.Instance().Update(addresses, SwitchMediaTime);
                lockService.UpdateSwitchMediaTime(SwitchMediaTime);
            }
            catch (Exception ex) 
            {
                PluginContext.Log.Error($"Get error while tring to save data {ex}");
                throw;
            }
            CloseAction?.Invoke();
        }

        public void Add()
        {
            if (AddressViewModels is null)
                AddressViewModels = new ObservableCollection<IAddressViewModel>();

            AddressViewModels.Add(new AddressViewModel(AddressViewModels.Count + 1, ""));
        }

        public void Remove()
        {
            AddressViewModels.Remove(SelectedAddress);
        }
    }
}
