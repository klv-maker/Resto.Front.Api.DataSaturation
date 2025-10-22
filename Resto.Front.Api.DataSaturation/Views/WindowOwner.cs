using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace Resto.Front.Api.DataSaturation.Views
{
    public class WindowOwner : IDisposable
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        private const string AppProcessName = "iikoFront.Net";
        private Window window;

        private void EntryPoint<T>(IViewModel viewModel) where T : Window, new()
        {
            window = WindowCreatorHelper.CreateWindow<T>();
            window.DataContext = viewModel;
            window.Loaded += (sender, args) =>
            {
                var runningProcesses = Process.GetProcessesByName(AppProcessName).SingleOrDefault();
                if (runningProcesses == null)
                    return;

                var frontHwnd = runningProcesses.MainWindowHandle;
                var currentHwnd = new WindowInteropHelper(window).Handle;
                SetParent(currentHwnd, frontHwnd);
            };

            if (viewModel is ISettingsViewModel settingsViewModel)
                settingsViewModel.CloseAction = () => Dispose();
            window.ShowDialog();
        }

        public void Dispose()
        {
            window.Dispatcher.Invoke(() =>
            {
                if (window.DataContext is ISettingsViewModel settingsViewModel)
                    settingsViewModel.CloseAction = null;

                window.Close();
                window.Dispatcher.InvokeShutdown();
            });
            window.Dispatcher.Thread.Join();
        }

        public void ShowDialog<T>(IViewModel viewModel) where T : Window, new()
        {
            var windowThread = new Thread(() => EntryPoint<T>(viewModel));
            windowThread.SetApartmentState(ApartmentState.STA);
            windowThread.Start();
        }
    }
}
