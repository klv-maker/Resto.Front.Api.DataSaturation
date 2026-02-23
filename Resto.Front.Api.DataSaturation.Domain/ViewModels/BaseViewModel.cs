using Resto.Front.Api.DataSaturation.Domain.Interfaces.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Resto.Front.Api.DataSaturation.Domain.ViewModels
{
    public class BaseViewModel : IViewModel
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
