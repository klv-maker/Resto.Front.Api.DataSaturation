using Resto.Front.Api.DataSaturation.Entities;
using Resto.Front.Api.DataSaturation.Helpers;
using Resto.Front.Api.DataSaturation.Interfaces.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows;

namespace Resto.Front.Api.DataSaturation.ViewModels
{
    public class LockViewModel : ILockViewModel
    {
        private Uri image;
        public Uri Image
        {
            get
            {
                return image;
            }
            set
            {
                image = value;
                OnPropertyChanged(nameof(Image));
            }
        }

        private Visibility imageVisible = Visibility.Collapsed;
        public Visibility ImageVisible
        {
            get
            {
                return imageVisible;
            }
            set
            {
                imageVisible = value;
                OnPropertyChanged(nameof(ImageVisible));
            }
        }

        private Uri gif;
        public Uri Gif
        {
            get
            {
                return gif;
            }
            set
            {
                gif = value;
                OnPropertyChanged(nameof(Gif));
            }
        }

        private Visibility gifVisible = Visibility.Collapsed;
        public Visibility GifVisible
        {
            get
            {
                return gifVisible;
            }
            set
            {
                gifVisible = value;
                OnPropertyChanged(nameof(GifVisible));
            }
        }

        private Uri video;
        public Uri Video
        {
            get
            {
                return video;
            }
            set
            {
                video = value;
                OnPropertyChanged(nameof(Video));
            }
        }

        private Visibility videoVisible = Visibility.Collapsed;
        public Visibility VideoVisible
        {
            get
            {
                return videoVisible;
            }
            set
            {
                videoVisible = value;
                OnPropertyChanged(nameof(videoVisible));
            }
        }
        public Action CloseAction { get; set; }

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
        public LockViewModel() { }

        public bool Update(int switchMediaTime)
        {
            if (isDisposed)
                return false;

            if (switchContentTimer is null)
                CreateTimer();

            switchContentTimer.Stop();
            if (switchMediaTime != 0)
                switchContentTimer.Interval = switchMediaTime * 1000;
            return AddMedia();
        }

        private bool AddMedia()
        {
            PluginContext.Log.Info($"[{nameof(LockViewModel)}|{nameof(AddMedia)}] Trying to add media");
            var path = Path.Combine(PluginContext.Integration.GetDataStorageDirectoryPath(), "Media");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            filesDictionary = Directory.EnumerateFiles(path).ToDictionary(file => file, file => MediaTypeHelper.GetMediaType(file));
            if (!filesDictionary.Any())
            {
                var assetsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Assets");
                if (!Directory.Exists(assetsPath))
                {
                    PluginContext.Log.Error($"[{nameof(LockViewModel)}|{nameof(AddMedia)}] Assets folder not found"); 
                    return false;
                }

                filesDictionary = Directory.EnumerateFiles(assetsPath).ToDictionary(file => file, file => MediaTypeHelper.GetMediaType(file));
                if (!filesDictionary.Any())
                    PluginContext.Log.Error($"[{nameof(LockViewModel)}|{nameof(AddMedia)}] Assets folder is empty");

                currentFileShownIndex = 0;
                var filePath = filesDictionary.ElementAt(currentFileShownIndex).Key;
                PluginContext.Log.Info($"[{nameof(LockViewModel)}|{nameof(AddMedia)}] Trying to show file {filePath}");
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
            var mediaType = MediaTypeHelper.GetMediaType(path);
            if (currentMedia == MediaType.Unknown)
                currentMedia = mediaType;

            switch (mediaType)
            {
                case MediaType.Unknown:
                    PluginContext.Log.Error($"[{nameof(LockViewModel)}|{nameof(UpdateMedia)}] Получили непонятный файл");
                    break;
                case MediaType.Video:
                    Video = new Uri(path);
                    if (VideoVisible != Visibility.Visible)
                        VideoVisible = Visibility.Visible;

                    if (ImageVisible is Visibility.Visible)
                        ImageVisible = Visibility.Collapsed;

                    if (GifVisible is Visibility.Visible)
                        GifVisible = Visibility.Collapsed;
                    break;
                case MediaType.Image:
                    Image = new Uri(path);
                    if (VideoVisible is Visibility.Visible)
                        VideoVisible = Visibility.Collapsed;

                    if (ImageVisible != Visibility.Visible)
                        ImageVisible = Visibility.Visible;

                    if (GifVisible is Visibility.Visible)
                        GifVisible = Visibility.Collapsed;
                    break;
                case MediaType.Gif:
                    Gif = new Uri(path);

                    if (VideoVisible is Visibility.Visible)
                        VideoVisible = Visibility.Collapsed;

                    if (ImageVisible is Visibility.Visible)
                        ImageVisible = Visibility.Collapsed;

                    if (GifVisible != Visibility.Visible)
                        GifVisible = Visibility.Visible;
                    break;
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

                PluginContext.Log.Info($"[{nameof(LockViewModel)}|{nameof(SwitchContentTimer_Elapsed)}] file {filePathKeyValuePair.Key} not found. Remove from dictionary");
                filesDictionary.Remove(filePathKeyValuePair.Key);
            }

            if (currentFileIndex == currentFileShownIndex)
            {
                switchContentTimer.Stop();
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
        }
    }
}
