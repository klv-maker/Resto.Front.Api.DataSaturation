using CommunityToolkit.Mvvm.Input;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces;
using Resto.Front.Api.DataSaturation.MindBox.Interfaces.VIewModels;
using Resto.Front.Api.DataSaturation.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.MindBox.ViewModels
{
    public class MindBoxSettingsViewModel : IMindBoxSettingsViewModel
    {
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
        private string key;
        public string Key
        {
            get
            {
                return key;
            }
            set 
            {
                key = value;
                OnPropertyChanged(nameof(Key));
            }
        }

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

        public Action CloseAction { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        public MindBoxSettingsViewModel(IMindBoxSettings mindBoxSettings) 
        {
            Update(mindBoxSettings);
        }

        private void Update(IMindBoxSettings mindBoxSettings)
        {
            if (mindBoxSettings == null)
                return;

            AddressApi = mindBoxSettings.AddressApi;
            Key = mindBoxSettings.Key;
        }

        public void Cancel()
        {
            CloseAction?.Invoke();
        }

        public void Save()
        {
            try
            {
                MindBoxSettings.Instance().Update(AddressApi, Key);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"Get error while tring to save data {ex}");
                throw;
            }
            CloseAction?.Invoke();
        }

    }
}
