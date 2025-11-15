using System;
using System.ComponentModel;

namespace Resto.Front.Api.DataSaturation.Interfaces.ViewModels
{
    public interface IViewModel : INotifyPropertyChanged
    {
        Action CloseAction { get; set; }
    }
}
