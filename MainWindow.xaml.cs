using System;
using TagLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
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
        private bool _isSliderDragging = false; // проверка взаимодействия пользователя с ползунком
        private Storyboard _rotationAnimation;
        private bool _isRotating;

        public MainWindow()
        {
            InitializeComponent();
            LoadDefaultAlbumArt(); // Загрузка  дефолтной обложки
            _rotationAnimation = (Storyboard)FindResource("RotationAnimation"); // загрузка анимации 
            _CurrentSong = 0;
            _Songs = LoadSongs(); // загружаем все треки
            _Player.MediaOpened += (s, e) =>
            {
                InitializeTimer();
                ParseSong();

                if (_Player.NaturalDuration.HasTimeSpan)
                {
                    SliderTimeCurrent.Maximum = _Player.NaturalDuration.TimeSpan.TotalSeconds;
                }
            };
            _Player.Open(new Uri(_Songs[_CurrentSong]));

        }

        private void InitializeTimer() // Инициализация таймера
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(200);
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e) // Тик таймера 
        {
            if (_Player.NaturalDuration.HasTimeSpan && _Player.NaturalDuration.TimeSpan.TotalSeconds > 0)
            {
                TimeSpan currentPosition = _Player.Position;
                SongTimeCurrent.Text = $"{(int)currentPosition.TotalMinutes}:{currentPosition.Seconds:D2}";

                if (!_isSliderDragging)
                {
                    if (Math.Abs(SliderTimeCurrent.Value - currentPosition.TotalSeconds) > 0.5)
                    {
                        SliderTimeCurrent.Value = currentPosition.TotalSeconds;
                    }
                }
            }
        }

        private string[] LoadSongs() // Загрузка списка песен
        {
            var fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content\\Music");
    
        // Проверяем существование директории
        if (!Directory.Exists(fullPath))
        {
            MessageBox.Show("Директория с музыкой не найдена!");
            return new string[0];
        }

        var songs = Directory.GetFiles(fullPath);
    
        // Проверяем, есть ли файлы
        if (songs.Length == 0)
        {
            MessageBox.Show("В директории нет музыкальных файлов!");
        }
    
        return songs;
        }

        private void LoadDefaultAlbumArt() // Загрузка дефолтной обложки (если в метаданных трека нет своей)
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

        private async void ParseSong() // Парсит метаданные треков
        {
            await Task.Run(() =>
            {
                var song = TagLib.File.Create(_Songs[_CurrentSong]);
                Dispatcher.Invoke(ResetRotation);
                Dispatcher.Invoke(() =>
                {
                    SongName.Text = song.Tag.Title ?? song.Name.Split('\\').Last();
                    var Performers = song.Tag.Performers;
                    if (Performers.Length == 0)
                    {
                        ArtistName.Text = "Unknown Artist";
                    }
                    else
                    {
                        ArtistName.Text = Performers[0].Split(',')[0];
                    }
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

        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        ///                                     Обработчики нажатий кнопок и прочего                                  ///
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        private void PlayButton(object sender, RoutedEventArgs e)
        {
            if (_Songs.Length == 0) { MessageBox.Show("Нет песен для проигрывания!"); return; } // поимка ошибки

            if (!_IsPlaying) // трек не играет
            {
                _Player.Play();
                _timer.Start();
                StartRotation();
                Timer_Tick(null, EventArgs.Empty);
            } else // трек играет
            {
                _Player.Pause();
                _timer.Stop();
                StopRotation();
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

        private void SkipButton(object sender, RoutedEventArgs e)
        {
            if (_IsPlaying)
            {
                _IsPlaying = false;
                _Player.Stop();
            }
            _CurrentSong = (_CurrentSong + 1) % _Songs.Length;

            _Player.Open(new Uri(_Songs[_CurrentSong]));
            ParseSong();
            ResetRotation();
            _Player.Play();
            _IsPlaying = true;
        }

        private void BackButton(object sender, RoutedEventArgs e)
        {
            if (_IsPlaying)
            {
                _IsPlaying = false;
                _Player.Stop();
            }
            var curSong = _CurrentSong - 1;
            if (curSong < 0)
            {
                _CurrentSong = _Songs.Length - 1;
            }
            else
            {
                _CurrentSong--;
            }

            _Player.Open(new Uri(_Songs[_CurrentSong]));
            ParseSong();
            ResetRotation();
            _Player.Play();
            _IsPlaying = true;
        }

        private void DragAndMove(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedEventArgs e)
        {
                _Player.Volume = VolumeSlider.Value;
        }

        private void SliderTimeCurrent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSliderDragging)
            {
                _Player.Position = TimeSpan.FromSeconds(e.NewValue);
            }
        }

        private void Slider_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isSliderDragging = true;
            _IsPlaying = false;
            _Player.Pause();
        }

        private void Slider_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isSliderDragging = false;
            _Player.Position = TimeSpan.FromSeconds(SliderTimeCurrent.Value);
            if (!_IsPlaying)
            {
                _IsPlaying = true;
                _Player.Play();
            }
        }

        private void StartRotation()
        {
            if (!_isRotating)
            {
                _rotationAnimation.Begin(this, true);
                _isRotating = true;
            }
        }

        private void StopRotation()
        {
            if (_isRotating)
            {
                _rotationAnimation.Pause(this);
                _isRotating = false;
            }
        }

        private void ResetRotation()
        {
            _rotationAnimation.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
            AlbumArtRotation.Angle = 0;
        }
    }
}
