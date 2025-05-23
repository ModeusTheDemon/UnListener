﻿using System;
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
using System.Windows.Controls;
using Microsoft.Win32;

namespace project
{
    public partial class MainWindow : Window
    {
        private bool _IsPlaying = false;
        private int _CurrentSong;
        private String[] _Songs;
        private readonly MediaPlayer _Player = new MediaPlayer();
        private DispatcherTimer _timer;
        private bool _isSliderDragging = false;
        private readonly Storyboard _rotationAnimation;
        private bool _isRotating;
        private bool _IsRepeating = false;
        private bool _IsSongListVisible = false;

        public MainWindow()
        {
            InitializeComponent();
            SizeChanged += Window_SizeChanged;
            LoadDefaultAlbumArt();
            _rotationAnimation = (Storyboard)FindResource("RotationAnimation");
            _Songs = LoadSongs();
            _CurrentSong = 0;

            _Player.MediaOpened += (s, e) =>
            {
                InitializeTimer();
                ParseSong();

                if (_Player.NaturalDuration.HasTimeSpan)
                    SliderTimeCurrent.Maximum = _Player.NaturalDuration.TimeSpan.TotalSeconds;
            };

            _Player.MediaEnded += OnMediaEnded;

            if (_Songs.Length > 0)
                _Player.Open(new Uri(_Songs[_CurrentSong]));
        }

        private void InitializeTimer()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!_Player.NaturalDuration.HasTimeSpan) return;

            var currentPosition = _Player.Position;
            SongTimeCurrent.Text = $"{(int)currentPosition.TotalMinutes}:{currentPosition.Seconds:D2}";

            if (!_isSliderDragging && Math.Abs(SliderTimeCurrent.Value - currentPosition.TotalSeconds) > 0.5)
                SliderTimeCurrent.Value = currentPosition.TotalSeconds;
        }

        private string[] LoadSongs()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content\\Music");

            if (!Directory.Exists(path))
            {
                MessageBox.Show("Директория с музыкой не найдена!");
                return Array.Empty<string>();
            }

            var songs = Directory.GetFiles(path);
            if (songs.Length == 0)
                MessageBox.Show("В директории нет музыкальных файлов!");

            return songs;
        }

        private void LoadDefaultAlbumArt()
        {
            var path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content/Pictures/default_bg.jpg");

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.DecodePixelWidth = 800;
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();

            AlbumArtBrush.ImageSource = bitmap;
            AlbumArtBrush.Stretch = Stretch.UniformToFill;
        }

        private async void ParseSong()
        {
            await Task.Run(() =>
            {
                var song = TagLib.File.Create(_Songs[_CurrentSong]);

                Dispatcher.Invoke(() =>
                {
                    ResetRotation();
                    SongName.Text = song.Tag.Title ?? System.IO.Path.GetFileNameWithoutExtension(_Songs[_CurrentSong]);

                    ArtistName.Text = song.Tag.Performers.Length > 0 ? song.Tag.Performers[0].Split(',')[0] : "Unknown Artist";

                    SongTimeLenght.Text = $"{(int)song.Properties.Duration.TotalMinutes}:{song.Properties.Duration.Seconds:D2}";

                    if (song.Tag.Pictures.Length > 0)
                    {
                        var ms = new MemoryStream(song.Tag.Pictures[0].Data.Data);
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                        AlbumArtBrush.ImageSource = bitmap;
                    }
                    else
                    {
                        LoadDefaultAlbumArt();
                    }
                });
            });
        }

        private void Shuffle(String[] list)
        {
            var rng = new Random();
            for (int i = list.Length - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private void PlayButton(object sender, RoutedEventArgs e)
        {
            CheckSongs();

            if (_IsPlaying)
            {
                _Player.Pause();
                _timer.Stop();
                StopRotation();
            }
            else
            {
                _Player.Play();
                _timer.Start();
                StartRotation();
                Timer_Tick(null, EventArgs.Empty);
            }

            _IsPlaying = !_IsPlaying;
        }

        private void CloseButton(object sender, RoutedEventArgs e) => Close();

        private void ResizeButton(object sender, RoutedEventArgs e)
        {
            if (Window.WindowState == WindowState.Maximized)
            {
                Window.WindowState = WindowState.Normal;
                Window.Height = 600.0;
                Window.Width = _IsSongListVisible ? 900.0 : 600.0;
            }
            else
            {
                Window.WindowState = WindowState.Maximized;
            }
        }

        private void MinimizeButton(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void SkipButton(object sender, RoutedEventArgs e) => NextSong();

        private void BackButton(object sender, RoutedEventArgs e) => PreviousSong();

        private void ShuffleButton(object sender, RoutedEventArgs e)
        {
            CheckSongs();
            if (_IsPlaying)
                PlayButton(null, new RoutedEventArgs());

            Shuffle(_Songs);
            _CurrentSong = 0;
            SongList();
            PlayNewSong();
        }

        private void RepeatButton(object sender, RoutedEventArgs e)
        {
            if (!_IsRepeating)
            {
                _IsRepeating = true;
                _Player.MediaEnded -= OnMediaEnded;
                _Player.MediaEnded += RepeatingMediaEnded;
                RepButton.Content = "";
            }
            else
            {
                _IsRepeating = false;
                _Player.MediaEnded -= RepeatingMediaEnded;
                _Player.MediaEnded += OnMediaEnded;
                RepButton.Content = "";
            }
        }

        private void ListButton(object sender, RoutedEventArgs e)
        {
            if (!_IsSongListVisible)
            {
                ExpandRightPanel();
                SongList();
            }
            else
            {
                CollapseRightPanel();
            }

            _IsSongListVisible = !_IsSongListVisible;
        }

        private void SettingsButton(object sender, RoutedEventArgs e)
        {
            if (cm != null)
            {
                cm.PlacementTarget = (UIElement)sender;
                cm.IsOpen = true;
            }
        }

        private void LoadSongButton(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите трек для добавления",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Filter = "Аудиофайлы|*.mp3;*.wav;*.flac;*.m4a", 
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    _Songs = _Songs.Concat(new[] { filename }).ToArray();
                    System.IO.File.Copy(filename, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Content\\Music\\", $"{filename.Split('\\').Last()}"));
                }

                SongList();
            }
        }

        private void DeleteSongButton(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите трек для удаления",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "Content\\Music\\",
                Filter = "Аудиофайлы|*.mp3;*.wav;*.flac;*.m4a",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filename in openFileDialog.FileNames)
                {
                    _Songs = _Songs.Where(x => x != filename).ToArray();
                    System.IO.File.Delete(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory + "Content\\Music\\", $"{filename.Split('\\').Last()}"));
                }

                SongList();
            }
        }

        private void DragAndMove(object sender, MouseButtonEventArgs e) => DragMove();

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => _Player.Volume = VolumeSlider.Value;

        private void SliderTimeCurrent_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isSliderDragging)
                _Player.Position = TimeSpan.FromSeconds(e.NewValue);
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
                PlayButton(null, new RoutedEventArgs());
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Window.WindowState == WindowState.Normal && _IsSongListVisible)
            {
                Window.Width = 900.0;
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

        private void NextSong()
        {
            CheckSongs();
            _CurrentSong = (_CurrentSong + 1) % _Songs.Length;
            PlayNewSong();
        }

        private void PreviousSong()
        {
            CheckSongs();
            _CurrentSong = (_CurrentSong - 1 + _Songs.Length) % _Songs.Length;
            PlayNewSong();
        }

        private void PlayNewSong()
        {
            _IsPlaying = false;
            _Player.Stop();
            _Player.Open(new Uri(_Songs[_CurrentSong]));
            ParseSong();
            ResetRotation();
            _timer.Start();
            Timer_Tick(null, EventArgs.Empty);
            _Player.Play();
            _IsPlaying = true;
            StartRotation();
            UpdateSongListHighlight();
        }

        private void CheckSongs()
        {
            if (_Songs.Length == 0)
            {
                MessageBox.Show("в списке нет песен!\nВы можете добавить их через настройки ;)");
                return;
            }
        }

        private void UpdateSongListHighlight()
        {
            foreach (Border songItem in SongListPanel.Children)
            {
                int index = (int)songItem.Tag;
                var grid = (Grid)songItem.Child;
                var playBtn = (Button)grid.Children[1];

                if (index == _CurrentSong)
                {
                    songItem.Background = new SolidColorBrush(Color.FromRgb(100, 100, 100));
                    playBtn.Content = "";
                    playBtn.Opacity = 1;
                }
                else
                {
                    songItem.Background = Brushes.Transparent;
                    playBtn.Content = "";
                    playBtn.Opacity = 0;
                }
            }
        }

        private void OnMediaEnded(object sender, EventArgs e) => SkipButton(null, new RoutedEventArgs());

        private void RepeatingMediaEnded(object sender, EventArgs e) => PlayNewSong();

        private void ExpandRightPanel()
        {
            var storyboard = new Storyboard();

            if (Window.WindowState != WindowState.Maximized)
            {
                var windowAnimation = new DoubleAnimation
                {
                    From = Window.ActualWidth,
                    To = Window.ActualWidth + 300,
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(windowAnimation, Window);
                Storyboard.SetTargetProperty(windowAnimation, new PropertyPath(FrameworkElement.WidthProperty));
                storyboard.Children.Add(windowAnimation);
            }

            var columnAnimation = new GridLengthAnimation
            {
                From = new GridLength(0),
                To = new GridLength(300),
                Duration = TimeSpan.FromMilliseconds(300)
            };
            Storyboard.SetTarget(columnAnimation, RightPanelColumn);
            Storyboard.SetTargetProperty(columnAnimation, new PropertyPath(ColumnDefinition.WidthProperty));
            storyboard.Children.Add(columnAnimation);

            storyboard.Begin();
        }

        private void CollapseRightPanel()
        {
            var storyboard = new Storyboard();

            if (Window.WindowState != WindowState.Maximized)
            {
                var windowAnimation = new DoubleAnimation
                {
                    From = Window.ActualWidth,
                    To = Window.ActualWidth - 300,
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(windowAnimation, Window);
                Storyboard.SetTargetProperty(windowAnimation, new PropertyPath(FrameworkElement.WidthProperty));
                storyboard.Children.Add(windowAnimation);
            }

            var columnAnimation = new GridLengthAnimation
            {
                From = new GridLength(300),
                To = new GridLength(0),
                Duration = TimeSpan.FromMilliseconds(300)
            };
            Storyboard.SetTarget(columnAnimation, RightPanelColumn);
            Storyboard.SetTargetProperty(columnAnimation, new PropertyPath(ColumnDefinition.WidthProperty));
            storyboard.Children.Add(columnAnimation);

            storyboard.Begin();
        }

        private void SongList()
        {
            SongListPanel.Children.Clear();

            for (int i = 0; i < _Songs.Length; i++)
            {
                var songName = System.IO.Path.GetFileNameWithoutExtension(_Songs[i]);
                var songItem = new Border
                {
                    Margin = new Thickness(5),
                    Background = Brushes.Transparent,
                    CornerRadius = new CornerRadius(10),
                    Cursor = Cursors.Hand,
                    Tag = i
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition());
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var text = new TextBlock
                {
                    Text = songName,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    Margin = new Thickness(10)
                };

                var playBtn = new Button
                {
                    Content = "",
                    FontSize = 14,
                    Foreground = Brushes.White,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Opacity = 0,
                    Margin = new Thickness(0, 0, 10, 0),
                };

                int index = i;
                playBtn.Click += (s, e) =>
                {
                    _CurrentSong = index;
                    PlayNewSong();
                };

                grid.Children.Add(text);
                grid.Children.Add(playBtn);
                Grid.SetColumn(playBtn, 1);

                songItem.Child = grid;

                songItem.MouseEnter += (s, e) =>
                {
                    var border = (Border)s;
                    if ((int)border.Tag != _CurrentSong)
                    {
                        border.Background = new SolidColorBrush(Color.FromRgb(64, 64, 64));
                    }
                    ((Button)((Grid)border.Child).Children[1]).Opacity = 1;
                };

                songItem.MouseLeave += (s, e) =>
                {
                    var border = (Border)s;
                    if ((int)border.Tag != _CurrentSong)
                    {
                        border.Background = Brushes.Transparent;
                    }
                    ((Button)((Grid)border.Child).Children[1]).Opacity = 0;
                };

                SongListPanel.Children.Add(songItem);
            }

            UpdateSongListHighlight();
        }
        
    }



    public class GridLengthAnimation : AnimationTimeline
    {
        public static readonly DependencyProperty FromProperty = DependencyProperty.Register("From", typeof(GridLength), typeof(GridLengthAnimation));
        public static readonly DependencyProperty ToProperty   = DependencyProperty.Register("To",   typeof(GridLength), typeof(GridLengthAnimation));

        public GridLength From
        {
            get => (GridLength)GetValue(FromProperty);
            set => SetValue(FromProperty, value);
        }

        public GridLength To
        {
            get => (GridLength)GetValue(ToProperty);
            set => SetValue(ToProperty, value);
        }

        // без перезаписи нижележещих функций работать не хочет
        public override Type TargetPropertyType => typeof(GridLength);

        public override object GetCurrentValue(object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            if (animationClock.CurrentProgress == null)
                return From;

            double fromVal = From.Value;
            double toVal = To.Value;
            double progress = animationClock.CurrentProgress.Value;

            return new GridLength(fromVal + (toVal - fromVal) * progress, GridUnitType.Pixel);
        }

        protected override Freezable CreateInstanceCore() => new GridLengthAnimation();
    }

}
