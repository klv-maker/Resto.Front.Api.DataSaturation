using System.Windows;

namespace Resto.Front.Api.DataSaturation.Helpers
{
    public static class WindowCreatorHelper
    {
        public static T CreateWindow<T>() where T : Window, new()
        {
            return new T()
            {
                Topmost = true,
                ResizeMode = ResizeMode.NoResize,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                WindowStyle = WindowStyle.None,
                ShowActivated = true,
                Top = 0,
                Left = 0,
            };
        }
    }
}
