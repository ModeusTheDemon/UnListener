using System;
using TagLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;

namespace project
{
    public partial class MainWindow : Window
    {
        private bool _IsPlaying = false; // текущее состояние трека
        private int _CurrentSong; // позиция текущего трека
        private String[] _Songs; // список треков
        private MediaPlayer _Player = new MediaPlayer(); // Плеер
        private DispatcherTimer _timer; // таймер

        public MainWindow()
        {
            InitializeComponent();
            // InitializeTimer(); // TODO: таймер начинает работать с запозданием
            LoadDefaultAlbumArt(); // Загрузка  дефолтной обложки
            _CurrentSong = 0;
            _Songs = LoadSongs(); // загружаем все треки

            // Подписываемся на событие открытия медиа
            _Player.MediaOpened += (s, e) =>
            {
                InitializeTimer();
                ParseSong();
            };

            _Player.Open(new Uri(_Songs[_CurrentSong])); // загружаем первый трек
            

        }

        private void InitializeTimer() // Инициализация таймера
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e) // Обработчик тика таймера
        {
            if (_Player.NaturalDuration.HasTimeSpan && _Player.NaturalDuration.TimeSpan.TotalSeconds > 0)
            {
                TimeSpan currentPosition = _Player.Position;
                SongTimeCurrent.Text = $"{(int)currentPosition.TotalMinutes}:{currentPosition.Seconds:D2}";
            }
        }

        private void SetSliderTick(double SongLenght) // установка длины тика слайдера
        {
            var SliderTick = SongLenght / 100;
        }

        private string[] LoadSongs() // загрузка списка песен
        {
            var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/Music/"); // директория с музыкой

            var Songs = Directory.GetFiles(fullPath);

            return Songs;
        }

        private void LoadDefaultAlbumArt() // загрузка дефолтной обложки (если в метаданных трека нет своей)
        {
            var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/backgrounds/default_bg.jpg");

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(fullPath, UriKind.Absolute);
            bitmap.DecodePixelWidth = 800; 
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            AlbumArtBrush.ImageSource = bitmap;
            AlbumArtBrush.Stretch = Stretch.UniformToFill;
        }

        private async void ParseSong() // парсит метаданные треков
        {
            await Task.Run(() =>
            {
                var song = TagLib.File.Create(_Songs[_CurrentSong]);

                Dispatcher.Invoke(() =>
                {
                    SongName.Text = song.Tag.Title ?? "Unknown Track";
                    ArtistName.Text = song.Tag.Performers[0].Split(',')[0] ?? "Unknown Artist";
                    SongTimeLenght.Text = $"{(int)song.Properties.Duration.TotalMinutes}:{song.Properties.Duration.Seconds:D2}";

                    if (song.Tag.Pictures.Length > 0)
                    {
                        using (var ms = new MemoryStream(song.Tag.Pictures[0].Data.Data))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            AlbumArtBrush.ImageSource = bitmap;
                        }
                    }
                    else
                    {
                        LoadDefaultAlbumArt();
                    }
                });
            });
        }

        private void PlayButton(object sender, RoutedEventArgs e)
        {
            if (!_IsPlaying) // трек не играет
            {
                _Player.Play();
                _timer.Start();

                Timer_Tick(null, EventArgs.Empty);
            } else // трек играет
            {
                _Player.Pause();
                _timer.Stop();
            }
            _IsPlaying = !_IsPlaying;
        }

        private void CloseButton(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ResizeButton(object sender, RoutedEventArgs e)
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }

        private void MinimizeButton(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void DragAndMove(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
                _Player.Volume = VolumeSlider.Value;
        }
    }
}
