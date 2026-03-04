using Newtonsoft.Json.Linq;
using Resto.Front.Api.DataSaturation.Domain.Models;
using Resto.Front.Api.DataSaturation.Domain.ViewModels;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using System;
using System.Linq;

namespace Resto.Front.Api.DataSaturation.ViewModels
{
    public class CustomerDetailsViewModel : BaseViewModel, ICustomerDetailsViewModel
    {
        private string phoneNumber;
        public string PhoneNumber
        {
            get
            {
                return phoneNumber;
            }
            set
            {
                phoneNumber = value;
                OnPropertyChanged(nameof(PhoneNumber));
            }
        }

        private string guestName;
        public string GuestName
        {
            get
            {
                return guestName;
            }
            set
            {
                guestName = value;
                OnPropertyChanged(nameof(GuestName));
            }
        }

        private decimal balance;
        public decimal Balance
        {
            get
            {
                return balance;
            }
            set
            {
                balance = value;
                OnPropertyChanged(nameof(Balance));
            }
        }

        public CustomerDetailsViewModel(CustomerInfo customerInfo) 
        { 
            Update(customerInfo);
        }

        public void Update(CustomerInfo customerInfo)
        {
            PhoneNumber = customerInfo.userData.phone;
            GuestName = $"{customerInfo.userData.lastName} {customerInfo.userData.name}";
            var wallet = customerInfo.userWallets.FirstOrDefault();
            if (wallet != null) 
            {
                Balance = wallet.balance;
            }
        }
    }
}
