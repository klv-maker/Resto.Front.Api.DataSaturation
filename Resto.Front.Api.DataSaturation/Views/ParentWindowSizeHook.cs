using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Resto.Front.Api.DataSaturation.Views
{
    public class ParentWindowSizeHook : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // Делегат для обработки событий
        public delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        // Константы событий
        private const uint EVENT_OBJECT_LOCATIONCHANGE = 0x800B;
        private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

        private IntPtr _hookId = IntPtr.Zero;
        private WinEventDelegate _delegate;
        private IntPtr _parentHandle;
        private Window _childWindow;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public override string ToString() => $"{Width}x{Height}";
        }

        public ParentWindowSizeHook(IntPtr parentHandle, Window childWindow)
        {
            _parentHandle = parentHandle;
            _childWindow = childWindow;
            _delegate = new WinEventDelegate(WinEventProc);

            // Получаем ID процесса родительского окна
            GetWindowThreadProcessId(parentHandle, out uint processId);

            // Устанавливаем хук на события изменения размера и положения
            _hookId = SetWinEventHook(EVENT_OBJECT_LOCATIONCHANGE, EVENT_OBJECT_LOCATIONCHANGE,
                IntPtr.Zero, _delegate, processId, 0, WINEVENT_OUTOFCONTEXT);

            if (_hookId == IntPtr.Zero)
            {
                throw new Exception("Не удалось установить хук на события окна");
            }

            PluginContext.Log.Info("NativeSizeHook успешно инициализирован");
        }

        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
            int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        {
            // Проверяем, что событие относится к нашему родительскому окну
            // idObject = 0 означает само окно, а не дочерний элемент
            if (hwnd == _parentHandle && idObject == 0)
            {
                GetWindowRect(_parentHandle, out RECT currentRect);

                PluginContext.Log.Info($"Обнаружено изменение размера родителя: {currentRect}");
                // Обновляем дочернее окно
                UpdateChildWindowSize(currentRect);
            }
        }

        private void UpdateChildWindowSize(RECT parentRect)
        {
            if (_childWindow == null) return;

            // Используем Dispatcher для безопасного обновления UI
            _childWindow.Dispatcher.Invoke(() =>
            {
                try
                {
                    _childWindow.Width = parentRect.Width / 3 * 2;
                    _childWindow.Height = parentRect.Height;
                    _childWindow.Top = parentRect.Top;
                    _childWindow.Left = parentRect.Left;
                }
                catch (Exception ex)
                {
                    PluginContext.Log.Info($"Ошибка при обновлении дочернего окна: {ex.Message}");
                }
            });
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWinEvent(_hookId);
                _hookId = IntPtr.Zero;
                PluginContext.Log.Info("NativeSizeHook отключен");
            }
        }
    }
}
