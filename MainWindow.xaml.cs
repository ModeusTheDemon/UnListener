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
        private bool _IsPlaying; // текущее состояние трека
        private int _CurrentSong; // позиция текущего трека
        private String[] _Songs; // список треков
        private MediaPlayer _Player = new MediaPlayer(); // Плеер
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTimer(); // TODO: таймер начинает работать с запозданием
            LoadDefaultAlbumArt();
            _IsPlaying = false;
            _CurrentSong = 0;
            _Songs = LoadSongs();
            _Player.Open(new Uri(_Songs[_CurrentSong])); // загружаем первый трек
            ParseSong();


        }

        private void InitializeTimer() // Инициализация таймера
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e) // Обработчик тика таймера
        {
            TimeSpan currentPosition = _Player.Position;

            SongTimeCurrent.Text = $"{(int)currentPosition.TotalMinutes}:{currentPosition.Seconds:D2}";
            var parsed_song_lenght = TimeSpan.Parse(SongTimeLenght.Text);
        }

        private void SetSliderTick(double SongLenght)
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

        private void ParseSong() // парсит метаданные треков
        {
            var song = TagLib.File.Create(_Songs[_CurrentSong]);

            SongName.Text = song.Tag.Title; // установка имени трека
            var performers_one_line = song.Tag.Performers[0];
            var performers = performers_one_line.Split(',');
            SongTimeLenght.Text = $"{(int)song.Properties.Duration.TotalMinutes}:{song.Properties.Duration.Seconds:D2}";
            ArtistName.Text = performers[0]; // установка имени исполнителя

            SetSliderTick(song.Properties.Duration.TotalSeconds); // установка тика слайдера

            // установка обложки альбома
            if (song.Tag.Pictures.Length == 0)
                LoadDefaultAlbumArt();

            IPicture picture = song.Tag.Pictures[0];

            using (var memoryStream = new MemoryStream(picture.Data.Data))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = memoryStream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                AlbumArtBrush.ImageSource = bitmap;
                AlbumArtBrush.Stretch = Stretch.UniformToFill;
            }
        }

        private void PlayButton(object sender, RoutedEventArgs e)
        {
            if (!_IsPlaying) // трек не играет
            {
                _Player.Play();
                _timer.Start();
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
