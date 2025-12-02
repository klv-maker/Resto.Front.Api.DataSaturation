using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
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
        private Window window;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        private void EntryPoint<T>(IViewModel viewModel) where T : Window, new()
        {
            try
            {
                window = WindowCreatorHelper.CreateWindow<T>();
                window.DataContext = viewModel;
                window.Loaded += OnLoaded;
                viewModel.CloseAction = () => Dispose();
                window.ShowDialog();

            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(WindowOwner)}|{nameof(EntryPoint)}] Get error when trying to create window: {ex}");
                Dispose();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            try
            {
                var runningProcesses = Process.GetProcessesByName(Constants.AppProcessName).SingleOrDefault();
                if (runningProcesses == null)
                    return;

                var frontHwnd = runningProcesses.MainWindowHandle;
                var currentHwnd = new WindowInteropHelper(window).Handle;
                SetParent(currentHwnd, frontHwnd);
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"[{nameof(WindowOwner)}|{nameof(OnLoaded)}] Get error on loaded: {ex}");
            }
        }

        public void Dispose()
        {
            if (window is null)
                return;

            if (window.Dispatcher is null)
                return;
            if (window.Dispatcher.HasShutdownStarted)
                return;

            window.Dispatcher.Invoke(() =>
            {
                if (window.DataContext is IViewModel viewModel)
                    viewModel.CloseAction = null;

                window.Loaded -= OnLoaded;
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
