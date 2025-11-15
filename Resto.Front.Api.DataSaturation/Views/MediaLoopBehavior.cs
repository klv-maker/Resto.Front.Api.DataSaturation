using Microsoft.Xaml.Behaviors;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Resto.Front.Api.DataSaturation.Views
{
    public class MediaLoopBehavior : Behavior<MediaElement>
    {
        private bool _isElementVisible;
        private bool _isDisposed = false;

        protected override void OnAttached()
        {
            base.OnAttached();

            _isElementVisible = AssociatedObject.IsVisible;

            // Подписываемся на события
            AssociatedObject.MediaEnded += LoopMedia;
            AssociatedObject.IsVisibleChanged += OnVisibilityChanged;
            AssociatedObject.Unloaded += OnUnloaded;
            AssociatedObject.LoadedBehavior = MediaState.Manual;

            var window = Window.GetWindow(AssociatedObject);
            if (window != null)
            {
                window.Closing += OnWindowClosing;
            }

            if (_isElementVisible && AssociatedObject.IsLoaded)
            {
                AssociatedObject.Play();
            }
        }

        protected override void OnDetaching()
        {
            CleanUp();
            base.OnDetaching();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CleanUp();
        }

        private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CleanUp();
        }

        private void CleanUp()
        {
            if (_isDisposed) return;

            try
            {
                if (AssociatedObject != null)
                {
                    // Отписываемся от всех событий
                    AssociatedObject.MediaEnded -= LoopMedia;
                    AssociatedObject.IsVisibleChanged -= OnVisibilityChanged;
                    AssociatedObject.Unloaded -= OnUnloaded;

                    // Останавливаем и очищаем медиа
                    AssociatedObject.Stop();
                    AssociatedObject.Close();

                    // Отписываемся от события окна
                    var window = Window.GetWindow(AssociatedObject);
                    if (window != null)
                    {
                        window.Closing -= OnWindowClosing;
                    }
                }
            }
            finally
            {
                _isDisposed = true;
            }
        }

        private void OnVisibilityChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _isElementVisible = (bool)e.NewValue;

            if (_isElementVisible && AssociatedObject.IsLoaded && !_isDisposed)
            {
                // Элемент стал видим - начинаем воспроизведение
                if (AssociatedObject.Position == AssociatedObject.NaturalDuration.TimeSpan)
                {
                    AssociatedObject.Position = TimeSpan.Zero;
                }
                AssociatedObject.Play();
            }
            else if (!_isElementVisible && !_isDisposed)
            {
                // Элемент стал невидим - останавливаем воспроизведение
                AssociatedObject.Pause();
            }
        }

        private void LoopMedia(object sender, RoutedEventArgs e)
        {
            // Зацикливаем только если элемент видим, загружен и не disposed
            if (_isElementVisible && AssociatedObject.IsLoaded && !_isDisposed)
            {
                AssociatedObject.Position = TimeSpan.Zero;
                AssociatedObject.Play();
            }
        }
    }
}
