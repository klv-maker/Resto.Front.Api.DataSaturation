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
        private const string AppProcessName = "iikoFront.Net";
        private Window window;
        private ParentWindowSizeHook _sizeHook;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        private void EntryPoint<T>(IViewModel viewModel) where T : Window, new()
        {
            window = WindowCreatorHelper.CreateWindow<T>();
            window.DataContext = viewModel;
            window.Loaded += OnLoaded;

            viewModel.CloseAction = () => Dispose();
            window.ShowDialog();
        }

        private void OnLoaded(object sender, RoutedEventArgs args)
        {
            var runningProcesses = Process.GetProcessesByName(AppProcessName).SingleOrDefault();
            if (runningProcesses == null)
                return;

            var frontHwnd = runningProcesses.MainWindowHandle;
            var currentHwnd = new WindowInteropHelper(window).Handle;
            SetParent(currentHwnd, frontHwnd);
            if (window is LockWindow)
            {
                GetWindowRect(frontHwnd, out var parentRect);
                window.Width = parentRect.Width / 3 * 2;
                window.Height = parentRect.Height;
                window.Top = parentRect.Top;
                window.Left = parentRect.Left;
                // Запускаем мониторинг изменений размера
                InitializeSizeHook(frontHwnd);
            }
        }

        public void Dispose()
        {
            _sizeHook?.Dispose();
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

        private void InitializeSizeHook(IntPtr parent)
        {
            try
            {
                _sizeHook = new ParentWindowSizeHook(parent, window);
                PluginContext.Log.Info("Мониторинг размера родительского окна активирован");
            }
            catch (Exception ex)
            {
                PluginContext.Log.Error($"Ошибка инициализации хука: {ex.Message}");
            }
        }
    }
}
