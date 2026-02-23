using CommunityToolkit.Mvvm.Input;
using Resto.Front.Api.DataSaturation.Domain.Models;
using Resto.Front.Api.DataSaturation.Domain.ViewModels;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using System;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.ViewModels
{
    public class CustomerViewModel : BaseViewModel, ICustomerViewModel
    {
        public Action CloseAction { get; set; }
        private ICommand returnCommand;
        public ICommand ReturnCommand
        {
            get
            {
                if (returnCommand == null)
                {
                    returnCommand = new RelayCommand(Return);
                }
                return returnCommand;
            }
        }

        private ICustomerDetailsViewModel customerDetailsViewModel;
        public ICustomerDetailsViewModel CustomerDetailsViewModel
        {
            get
            {
                return customerDetailsViewModel;
            }
            set
            {
                if (customerDetailsViewModel != value)
                    customerDetailsViewModel = value;
                OnPropertyChanged(nameof(CustomerDetailsViewModel));
            }
        }

        private bool isActiveRightColumn = false;
        public bool IsActiveRightColumn
        {
            get
            {
                return isActiveRightColumn; 
            } 
            set 
            {
                isActiveRightColumn = value;
                OnPropertyChanged(nameof(IsActiveRightColumn));
            }
        }

        private CustomerInfo customerInfo;

        public CustomerViewModel(CustomerInfo customerInfo) 
        {
            Update(customerInfo);            
        }

        private void Update(CustomerInfo customer)
        {
            customerInfo = customer;
            CustomerDetailsViewModel = new CustomerDetailsViewModel(customer);
        }

        private void Return()
        {
            CloseAction?.Invoke();
        }
    }
}
