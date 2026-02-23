using Resto.Front.Api.DataSaturation.Domain.Interfaces.ViewModels;
using System.Windows.Input;

namespace Resto.Front.Api.DataSaturation.Interfaces.ViewModels
{
    public interface ICustomerViewModel : IClosebaleViewModel
    {
        ICustomerDetailsViewModel CustomerDetailsViewModel { get; set; }
        bool IsActiveRightColumn { get; set; }
        ICommand ReturnCommand { get; }
    }
}
