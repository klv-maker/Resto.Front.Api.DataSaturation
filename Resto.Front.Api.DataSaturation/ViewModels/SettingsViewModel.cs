using CommunityToolkit.Mvvm.Input;
using Resto.Front.Api.DataSaturation.Domain.ViewModels;
using Resto.Front.Api.DataSaturation.Interfaces;
using Resto.Front.Api.DataSaturation.Interfaces.Services;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using Resto.Front.Api.DataSaturation.Settings;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.ViewModels
{
    public class SettingsViewModel : BaseViewModel, ISettingsViewModel
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
        private bool enableOrdersService;
        public bool EnableOrdersService
        {
            get
            {
                return enableOrdersService;
            }
            set
            {
                enableOrdersService = value;
                OnPropertyChanged(nameof(EnableOrdersService));
            }
        }
        private string dataQR;
        public string DataQR
        {
            get
            {
                return dataQR;
            }
            set
            {
                dataQR = value;
                OnPropertyChanged(nameof(DataQR));
            }
        }

        private IikoCard iikoCard;
        public IikoCard IikoCard
        {
            get 
            {
                return iikoCard;
            }
            set 
            {
                iikoCard = value;
                OnPropertyChanged(nameof(IikoCard));
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

        private IOrdersService orderService;
        private IIikoCardService iikoCardService;

        public SettingsViewModel(IOrdersService orderService, IIikoCardService iikoCardService, ISettings settings) 
        {
            this.orderService = orderService;
            this.iikoCardService = iikoCardService;
            Update(settings);
        }

        private void Update(ISettings settings)
        {
            for (var i = 0; i < settings.AdressesApi.Count; i++)
            {
                PluginContext.Log.Info($"Trying to add address: {settings.AdressesApi[i]}");
                AddressViewModels.Add(new AddressViewModel(i + 1, settings.AdressesApi[i], Remove));
            }
            SwitchMediaTime = settings.SwitchMediaTime;
            EnableOrdersService = settings.EnableOrdersService;
            DataQR = settings.DataQR;
            IikoCard = settings.IikoCard;
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
                Settings.Settings.Instance().Update(addresses, SwitchMediaTime, EnableOrdersService, DataQR, IikoCard);
                orderService.UpdateSettings(EnableOrdersService, DataQR);
                iikoCardService.UpdateSettings(IikoCard);
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

            AddressViewModels.Add(new AddressViewModel(AddressViewModels.Count + 1, "", Remove));
        }

        public void Remove(IAddressViewModel addressViewModel)
        {
            AddressViewModels.Remove(addressViewModel);
            //пересчитываем индексы
            for (var i = 0; i < AddressViewModels.Count; i++)
            {
                addressViewModels[i].UpdateIndex(i + 1);
            }
        }
    }
}
