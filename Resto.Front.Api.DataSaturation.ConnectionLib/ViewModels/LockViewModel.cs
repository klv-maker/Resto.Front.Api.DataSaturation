using NLog;
using Resto.Front.Api.DataSaturation.ConnectionLib.Entities;
using Resto.Front.Api.DataSaturation.ConnectionLib.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfAnimatedGif;
using static System.Environment;

namespace Resto.Front.Api.DataSaturation.ConnectionLib.ViewModels
{
    public class LockViewModel : ILockViewModel
    {
        public Action CloseAction { get; set; }
        private MediaElement mediaElement;
        private Grid grid;

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private Timer switchContentTimer;
        private bool isDisposed = false;
        /// <summary>
        /// список всех файлов для отображения
        /// </summary>
        private Dictionary<string, MediaType> filesDictionary = new Dictionary<string, MediaType>();
        private int currentFileShownIndex;
        private MediaType currentMedia;
        private readonly Logger logger;
        private readonly int switchMediaTime;
        private readonly string dataPath;
        private Image gifImage;
        public LockViewModel(Logger logger, MediaElement mediaElement, Grid grid) 
        {
            this.logger = logger;
            this.mediaElement = mediaElement;
            if (this.mediaElement != null)
                this.mediaElement.MediaEnded += VideoEnded;

            this.grid = grid;
            string pluginDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); // папка плагина
            string pluginsRootDirectory = Directory.GetParent(pluginDirectory).Name;
            var appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            logger.Info($"[{nameof(LockViewModel)}|.ctor] appdata = {appdata}");
            dataPath = Path.Combine(appdata, "iiko\\CashServer\\EntitiesStorage\\Plugins", pluginsRootDirectory);
            logger.Info($"[{nameof(LockViewModel)}|.ctor] dataPath = {dataPath}");
            if (Directory.Exists(dataPath))
            {
                var configPath = Path.Combine(dataPath, "settings.txt");
                if (File.Exists(configPath))
                {
                    var configValue = File.ReadAllText(configPath);
                    if (!int.TryParse(configValue, out switchMediaTime))
                        logger.Error($"[{nameof(LockViewModel)}|.ctor] Error reading config file. File value {configValue}");
                    else
                        logger.Info($"[{nameof(LockViewModel)}|.ctor] Get switch media time from settings {switchMediaTime}");
                }
                else
                    logger.Info($"[{nameof(LockViewModel)}|.ctor] Not found file {configPath}");
            }
            Update(switchMediaTime);
        }

        private bool Update(int switchMediaTimeTicks)
        {
            if (isDisposed)
                return false;

            if (switchContentTimer is null)
                CreateTimer();

            switchContentTimer.Stop();
            if (switchMediaTimeTicks != 0)
                switchContentTimer.Interval = switchMediaTimeTicks * 1000;
            return AddMedia();
        }

        private bool AddMedia()
        {
            logger.Info($"[{nameof(LockViewModel)}|{nameof(AddMedia)}] Trying to add media");

            var mediaPath = Path.Combine(dataPath, "Media");
            if (!Directory.Exists(mediaPath))
                Directory.CreateDirectory(mediaPath);

            filesDictionary = Directory.EnumerateFiles(mediaPath).ToDictionary(file => file, file => MediaTypeHelper.GetMediaType(file));
            if (!filesDictionary.Any())
            {
                var defaultPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
                var files = Directory.EnumerateFiles(defaultPath);
                if (files.Any())
                {
                    foreach (var file in files)
                    {
                        var mediaType = MediaTypeHelper.GetMediaType(file);
                        if (mediaType != MediaType.Unknown)
                            filesDictionary.Add(file, mediaType);
                    }
                }

                if (!filesDictionary.Any())
                {
                    logger.Error($"[{nameof(LockViewModel)}|{nameof(AddMedia)}] resources folder is empty"); 
                    return false;
                }

                currentFileShownIndex = 0;
                var filePath = filesDictionary.ElementAt(currentFileShownIndex).Key;
                UpdateMedia(filePath);

                //не стартуем таймер, если у таймера время смены меньше секунды, а то психодел какой-то
                if (filesDictionary.Count > 1 && switchContentTimer.Interval >= 1000)
                    switchContentTimer.Start();
                return true;
            }

            currentFileShownIndex = 0;
            //не стартуем таймер, если у таймера время смены меньше секунды, а то психодел какой-то
            if (filesDictionary.Count > 1 && switchContentTimer.Interval >= 1000)
                    switchContentTimer.Start();

            UpdateMedia(filesDictionary.ElementAt(currentFileShownIndex).Key);
            return true;
        }

        private void UpdateMedia(string path)
        {
            logger.Info($"[{nameof(LockViewModel)}|{nameof(UpdateMedia)}] Trying to show {path}");
            var mediaType = MediaTypeHelper.GetMediaType(path);
            if (currentMedia == MediaType.Unknown)
                currentMedia = mediaType;

            if (currentMedia == MediaType.Unknown) 
                return;

            if (mediaType == MediaType.Video)
            {
                if (mediaElement is null)
                    return;

                mediaElement.Dispatcher.BeginInvoke(new Action(() =>
                {
                    mediaElement.Source = new Uri(path);
                    mediaElement.Play();
                }));
                return;
            }

            if (mediaType != MediaType.Video)
            {
                if (mediaType == MediaType.Gif)
                {
                    mediaElement.Dispatcher.BeginInvoke(new Action(() => 
                    { 
                        SetImageGif(path);
                    }));

                    return;
                }
                if (grid is null)
                    return;

                // Сохраняем оригинальный источник для возможного восстановления
                grid.Dispatcher.BeginInvoke(new Action(() => 
                { 
                    if (grid.Background is null || !(grid.Background is ImageBrush backgroundBrush))
                        return;
                        Brush originalSource = grid.Background;

                    try
                    {
                        var image = CreateImageBrush(path, backgroundBrush);
                        if (image != null)
                            grid.Background = image;
                    }
                    catch (Exception ex)
                    {
                        logger?.Error($"Error replacing image source: {ex}");
                        grid.Background = originalSource;
                    }
                }));
            }        
        }

        private void VideoEnded(object sender, System.Windows.RoutedEventArgs e)
        {
            mediaElement.Dispatcher.BeginInvoke(new Action(() => {
                mediaElement.Position = TimeSpan.Zero;
            }));

            var oldIntervalIndex = currentFileShownIndex;
            SwitchContent();
            if (oldIntervalIndex == currentFileShownIndex)
            {
                mediaElement.Dispatcher.BeginInvoke(new Action(() => {
                    mediaElement.Play();
                }));
            }
        }

        private ImageBrush CreateImageBrush(string filePath, ImageBrush backgroundBrush)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    ImageBrush imageBrush = new ImageBrush();
                    imageBrush.ImageSource = bitmap;
                    imageBrush.Stretch = backgroundBrush.Stretch;
                    imageBrush.AlignmentX = backgroundBrush.AlignmentX;
                    imageBrush.AlignmentY = backgroundBrush.AlignmentY;

                    logger?.Info($"Created ImageBrush from file: {filePath}");
                    return imageBrush;
                }
                return null;
            }
            catch (Exception ex)
            {
                logger?.Error($"Error creating ImageBrush: {ex}");
                return null;
            }
        }
        private void CreateTimer()
        {
            switchContentTimer = new Timer();
            switchContentTimer.Enabled = true;
            switchContentTimer.AutoReset = true;
            switchContentTimer.Elapsed += SwitchContentTimer_Elapsed;
        }

        private void SwitchContentTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            SwitchContent();
        }

        private void SwitchContent()
        {
            if (isDisposed)
                return;

            if (!filesDictionary.Any())
                return;

            var currentFileIndex = currentFileShownIndex + 1;
            var zeroIsSet = false;
            while (true)
            {
                if (filesDictionary.Count <= currentFileIndex)
                {
                    if (zeroIsSet)
                        break;

                    zeroIsSet = true;
                    currentFileIndex = 0;
                }

                var filePathKeyValuePair = filesDictionary.ElementAt(currentFileIndex);
                if (filePathKeyValuePair.Value != currentMedia)
                {
                    currentFileIndex++;
                    continue;
                }

                if (File.Exists(filePathKeyValuePair.Key))
                    break;

                logger.Info($"[{nameof(LockViewModel)}|{nameof(SwitchContent)}] file {filePathKeyValuePair.Key} not found. Remove from dictionary");
                filesDictionary.Remove(filePathKeyValuePair.Key);
            }

            if (currentFileIndex == currentFileShownIndex)
            {
                switchContentTimer?.Stop();
                return;
            }

            currentFileShownIndex = currentFileIndex;
            UpdateMedia(filesDictionary.ElementAt(currentFileShownIndex).Key);
        }

        public void Close()
        {
            switchContentTimer?.Stop();
            CloseAction?.Invoke();
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            if (switchContentTimer is null)
                return;

            switchContentTimer.Stop();
            switchContentTimer.Elapsed -= SwitchContentTimer_Elapsed;
            switchContentTimer.Dispose();
            if (mediaElement != null)
                mediaElement.MediaEnded -= VideoEnded;
        }

        public void SetImageGif(string gifPath)
        {
            try
            {
                if (gifImage != null)
                {
                    SetGifImage(gifPath);
                    return;
                }

                logger.Info($"{mediaElement.Parent.GetType().Name}");
                if (mediaElement.Parent is Grid parent)
                {
                    // Создаем Image для GIF
                    gifImage = new Image();
                    gifImage.HorizontalAlignment = mediaElement.HorizontalAlignment;
                    gifImage.VerticalAlignment = mediaElement.VerticalAlignment;
                    gifImage.StretchDirection = mediaElement.StretchDirection;
                    gifImage.Stretch = mediaElement.Stretch;

                    gifImage.SetValue(Grid.RowProperty, mediaElement.GetValue(Grid.RowProperty));
                    gifImage.SetValue(Grid.ColumnProperty, mediaElement.GetValue(Grid.ColumnProperty));
                    gifImage.SetValue(Grid.RowSpanProperty, mediaElement.GetValue(Grid.RowSpanProperty));
                    gifImage.SetValue(Grid.ColumnSpanProperty, mediaElement.GetValue(Grid.ColumnSpanProperty));

                    SetGifImage(gifPath);
                    logger?.Info($"Animated GIF loaded: {gifPath}");

                    // Заменяем MediaElement на Image
                    int index = parent.Children.IndexOf(mediaElement);
                    parent.Children.RemoveAt(index);
                    parent.Children.Insert(index, gifImage);

                    logger?.Info("MediaElement replaced with animated GIF Image");
                }
            }
            catch (Exception ex)
            {
                logger?.Error($"Error in ReplaceMediaWithGif: {ex}");
            }
        }

        private void SetGifImage(string gifPath)
        {
            // Устанавливаем GIF как источник
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(gifPath, UriKind.Absolute);
            bitmap.EndInit();

            ImageBehavior.SetAnimatedSource(gifImage, bitmap);
        }
    }
}
