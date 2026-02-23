using Resto.Front.Api.DataSaturation.Domain.Interfaces.ViewModels;
using Resto.Front.Api.DataSaturation.Domain.Models;

namespace Resto.Front.Api.DataSaturation.Interfaces.ViewModels
{
    public interface ICustomerDetailsViewModel : IViewModel
    {
        string PhoneNumber { get; set; }
        string GuestName { get; set; }
        decimal Balance { get; set; }
        void Update(CustomerInfo customerInfo);
    }
}
