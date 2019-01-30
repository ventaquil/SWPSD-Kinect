using CSCore.CoreAudioAPI;
using CSCore.Streams;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Input;
using Microsoft.Kinect.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kinect
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string PlaylistDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Playlist");

        private Playlist Playlist = new Playlist();

        private int TimeSlider_Value = 0;

        private bool IsDragging = false;

        private System.Drawing.Size SpectrumImageSize;

        public MainWindow()
        {
            InitializeComponent();
            InitializeKinect();
            InitializePlaylist();
            InitializeTracks();
            InitializeBackgroundThreads();

            SpectrumImageSize = new System.Drawing.Size((int)VisualisationImage.Width, (int)VisualisationImage.Height);
        }

        private void InitializeBackgroundThreads()
        {
            BackgroundWorker positionBackgroundWorker = new BackgroundWorker();
            positionBackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                while (true)
                {
                    if (!IsDragging && (Playlist.WaveSource != null))
                    {
                        UpdateSliderValue(Playlist.Position);
                    }
                    Thread.Sleep(100); // sleep for 100ms
                }
            };
            positionBackgroundWorker.RunWorkerAsync();

            BackgroundWorker volumeBackgroundWorker = new BackgroundWorker();
            volumeBackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                while (true)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        if (Playlist.WaveSource != null)
                        {
                            try
                            {
                                Playlist.Volume = (int)VolumeSlider.Value;
                            }
                            catch (InvalidOperationException)
                            {
                            }
                        }
                    }));
                    Thread.Sleep(100); // sleep for 100ms
                }
            };
            volumeBackgroundWorker.RunWorkerAsync();

            BackgroundWorker spectrumBackgroundWorker = new BackgroundWorker();
            spectrumBackgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                while (true)
                {
                    if ((Playlist.SpectrumProvider != null) && (Playlist.WaveSource != null))
                    {
                        Bitmap bitmap;

                        LineSpectrum llineSpectrum = new LineSpectrum(Playlist.SpectrumProvider.FftSize)
                        {
                            SpectrumProvider = Playlist.SpectrumProvider,
                            UseAverage = true,
                            BarCount = 50,
                            BarSpacing = 2,
                            IsXLogScale = true,
                            ScalingStrategy = ScalingStrategy.Linear
                        };

                        if ((bitmap = llineSpectrum.CreateSpectrumLine(SpectrumImageSize, System.Drawing.Color.Green, System.Drawing.Color.Red, System.Drawing.Color.Black, true)) != null)
                        {
                            MemoryStream memoryStream = new MemoryStream();
                            bitmap.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Bmp);

                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.StreamSource = memoryStream;
                                bitmapImage.EndInit();

                                VisualisationImage.Source = bitmapImage;
                            }));
                        }
                    }

                    Thread.Sleep(100); // sleep for 100ms
                }
            };
            spectrumBackgroundWorker.RunWorkerAsync();
        }

        private void UpdateSliderValue(double position)
        {
            UpdateSliderValue((int)Math.Round(position * Playlist.Length));
        }

        private void UpdateSliderValue(int position)
        {
            TimeSlider_Value = position <= Playlist.Length ? position : Playlist.Length;

            if (Playlist.Length > 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    TimeSlider.Value = TimeSlider_Value / ((double)Playlist.Length);
                }));
            }
        }

        private void InitializeTracks()
        {
            if (Playlist.GetTracks().Length > 0)
            {
                BackgroundWorker backgroundWorker = new BackgroundWorker();
                backgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    Playlist.LoadTrack();
                };
                backgroundWorker.RunWorkerAsync();

                TimeSlider.IsEnabled = true;
            }

            Tracks.Children.Clear();

            int index = 0;
            foreach (Track track in Playlist.GetTracks())
            {
                Tracks.Children.Add(new Label()
                {
                    Content = track.Title,
                    FontWeight = (index++ == Playlist.CurrentTrack) ? FontWeights.Bold : FontWeights.Normal
                });
            }
        }

        private void InitializeKinect()
        {
            KinectRegion.SetKinectRegion(this, KinectRegionXAML);
            KinectRegionXAML.Loaded += KinectRegionXAML_Loaded;
            KinectRegionXAML.KinectSensor = KinectSensor.GetDefault();
            KinectRegionXAML.KinectSensor.Open();
        }

        private void InitializePlaylist()
        {
            Playlist.Clear();

            foreach (string path in Directory.GetFiles(PlaylistDirectory))
            {
                Track track = new Track(path);

                Playlist.Add(track);
            }

            Playlist.TrackChanged += Playlist_TrackChanged;
        }

        private void Playlist_TrackChanged(object sender, int e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                int index = 0;
                foreach (Label label in Tracks.Children)
                {
                    label.FontWeight = (index++ == Playlist.CurrentTrack) ? FontWeights.Bold : FontWeights.Normal;
                }
            }));
        }

        private void KinectRegionXAML_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Kinect Loaded");
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist.Play();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist.Stop();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist.Pause();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Playlist.Stop();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist.Next();
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            Playlist.Previous();
        }

        private void TimeSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (!Playlist.IsPlaying)
            {
                Playlist.PlayOnce();
            }

            UpdateSliderValue(TimeSlider.Value);
            Playlist.Position = TimeSlider_Value;

            IsDragging = false;
        }

        private void TimeSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            IsDragging = true;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "MP3 Files (*.mp3)|*.mp3|WAV Files (*.wav)|*.wav";

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string playlistDirectory = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Playlist");

                string filename = System.IO.Path.GetFileName(dialog.FileName);

                string oldFilePath = dialog.FileName;
                string newFilePath = System.IO.Path.Combine(playlistDirectory, filename);
                File.Copy(oldFilePath, newFilePath, true);

                InitializePlaylist();
                InitializeTracks();
            }
        }
    }
}
