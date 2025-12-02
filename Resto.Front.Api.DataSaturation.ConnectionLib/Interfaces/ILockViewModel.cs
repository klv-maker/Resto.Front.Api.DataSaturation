using System;
using System.ComponentModel;

namespace Resto.Front.Api.DataSaturation.ConnectionLib.Interfaces
{
    public interface ILockViewModel : IViewModel
    {
    }
    public interface IViewModel : INotifyPropertyChanged, IDisposable
    {
        Action CloseAction { get; set; }
    }
}
