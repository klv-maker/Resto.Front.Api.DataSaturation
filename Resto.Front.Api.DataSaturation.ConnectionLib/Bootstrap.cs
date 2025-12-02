using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using Resto.Front.Api.DataSaturation.ConnectionLib.Interfaces;
using Resto.Front.Api.DataSaturation.ConnectionLib.ViewModels;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Resto.Front.Api.DataSaturation.ConnectionLib
{
    public static class Bootstrap
    {
        private static BlockingCollection<IViewModel> viewModels = new BlockingCollection<IViewModel>();
        public static Logger logger;
        private static bool isDisposed = false;

        public static void Run()
        {
            var app = System.Windows.Application.Current;
            if (app == null)
            {
                logger?.Warn("Application.Current is null");
                return;
            }

            try
            {
                var pathNlogConfig = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NLog.config");
                if (File.Exists(pathNlogConfig))
                    LogManager.Configuration = new XmlLoggingConfiguration(pathNlogConfig);
                else
                {
                    // Создаем минимальную конфигурацию программно
                    var config = new LoggingConfiguration();
                    var fileTarget = new NLog.Targets.FileTarget("file")
                    {
                        FileName = "C:\\Temp\\iiko_connection_lib.log",
                        Layout = "${longdate} ${level:uppercase=true} ${message} ${exception:format=tostring}"
                    };
                    config.AddRule(LogLevel.Info, LogLevel.Fatal, fileTarget);
                    LogManager.Configuration = config;
                }

                logger = LogManager.GetLogger("ConnectionLib");
                logger.Info($"Start library at {DateTime.Now:O}");

                app.Dispatcher.BeginInvoke(new Action(() =>
                {
                    CheckExistingWindows(app);
                }));
            }
            catch (Exception ex)
            {
                // Резервное логирование в файл
                File.WriteAllText("C:\\Temp\\iiko_bootstrap_error.log",
                    $"Bootstrap error: {ex}\n{ex.StackTrace}");
            }
        }

        private static void CheckExistingWindows(Application app)
        {
            try
            {
                logger?.Info($"Checking {app.Windows.Count} windows");
                foreach (Window w in app.Windows)
                {
                    // Проверяем, что это MainWindow
                    if (!IsMainWindow(w))
                        continue;

                    try
                    {
                        logger?.Info($"Found window: {w.GetType().Name}, Title: {w.Title}, Name: {w.Name}");
                        w.Closed += WindowClosed;
                        // Ждем полной загрузки окна
                        if (!w.IsLoaded)
                            w.Loaded += (s, e) => ReplaceMedia(w);
                        else
                            ReplaceMedia(w);
                    }
                    catch (Exception ex)
                    {
                        logger?.Error($"Error processing window {w.Name}: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.Error($"Error in CheckExistingWindows: {ex}");
            }
        }

        private static void WindowClosed(object sender, EventArgs e)
        {
            logger?.Warn("WindowClosed");
            DisposeLib();
        }

        private static void ReplaceMedia(Window w)
        {
            var grid = SearchMainWindowGridWithImageBrush(w);
            var mediaElement = SearchMediaElementInWindow(w);
            var viewModel = new LockViewModel(logger, mediaElement, grid);
        }

        private static MediaElement SearchMediaElementInWindow(Window window)
        {
            try
            {
                logger?.Info($"Searching for MediaElement in window: {window.Title}");

                // Рекурсивный поиск MediaElement
                var mediaElements = FindVisualChildren<MediaElement>(window);
                int foundCount = 0;

                foreach (var media in mediaElements)
                {
                    foundCount++;
                    logger?.Info($"Found MediaElement #{foundCount}: Name='{media.Name}', Parent={media.Parent?.GetType().Name}");

                    // Если у MediaElement есть имя, логируем его
                    if (!string.IsNullOrEmpty(media.Name))
                        logger?.Info($"MediaElement name: {media.Name}");

                    if (media.Parent != null) // Убедимся, что элемент в визуальном дереве
                        return media;
                }
                return null;
            }
            catch (Exception ex)
            {
                logger?.Error($"Error searching in window {window.Title}: {ex}");
                return null;
            }
        }

        // Рекурсивный поиск всех элементов указанного типа в визуальном дереве
        private static System.Collections.Generic.IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        // Рекурсивный поиск элемента по имени во всем визуальном дереве
        private static T FindVisualChildByName<T>(DependencyObject parent, string name) where T : FrameworkElement
        {
            if (parent == null) 
                return null;

            T foundElement = null;

            // Проверяем сам родительский элемент
            if (parent is T frameworkElement && frameworkElement.Name == name)
                return frameworkElement;

            // Рекурсивно проверяем детей
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                foundElement = FindVisualChildByName<T>(child, name);
                if (foundElement != null)
                    break;
            }

            return foundElement;
        }

        private static Grid SearchMainWindowGridWithImageBrush(Window window)
        {
            try
            {
                // Ищем все Grid в MainWindow
                var grids = FindVisualChildren<Grid>(window).ToList();
                logger?.Info($"Found {grids.Count} Grid elements in MainWindow");
                return grids?.FirstOrDefault(grid => grid.Background != null && grid.Background is ImageBrush);

            }
            catch (Exception ex)
            {
                logger?.Error($"Error searching MainWindow Grid: {ex}");
                return null;
            }
        }

        // Метод для определения, является ли окно MainWindow
        private static bool IsMainWindow(Window window)
        {
            if (!string.Equals(window.Name, "mainWindow", StringComparison.OrdinalIgnoreCase))
                return false;

            logger?.Info($"Identified as MainWindow: Title={window.Title}, Name={window.Name}");
            return true;
        }

        public static void DisposeLib()
        {
            if (isDisposed)
                return;

            try
            {
                logger?.Info("Disposing library...");

                viewModels.CompleteAdding();
                foreach (var item in viewModels)
                    item.Dispose();
            }
            catch (Exception ex)
            {
                logger?.Error($"Error during dispose: {ex}");
            }
            finally
            {
                viewModels.Dispose();
                isDisposed = true;
                logger?.Info("Library disposed successfully");
            }
        }
    }
}
