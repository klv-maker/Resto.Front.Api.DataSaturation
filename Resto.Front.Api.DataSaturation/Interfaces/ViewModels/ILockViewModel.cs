using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Resto.Front.Api.DataSaturation.Interfaces.ViewModels
{
    public interface ILockViewModel : IViewModel, IDisposable
    {
        Uri Image { get; set; }
        Visibility ImageVisible { get; set; }
        Uri Gif { get; set; }
        Visibility GifVisible { get; set; }
        Uri Video { get; set; }
        Visibility VideoVisible { get; set; }
        bool Update(int switchMediaTime);
    }
}
