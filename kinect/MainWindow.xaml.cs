using CSCore.CoreAudioAPI;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.Input;
using Microsoft.Kinect.Wpf.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        public MainWindow()
        {
            InitializeComponent();
            InitializeKinect();
            InitializePlaylist();
            InitializeTracks();
            InitializeBackgroundThreads();
        }

        private void InitializeBackgroundThreads()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (!IsDragging)
                    {
                        UpdateSliderValue(Playlist.Position);
                    }
                    Thread.Sleep(100); // sleep for 100ms
                }
            });

            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += (object sender, DoWorkEventArgs e) =>
            {
                while (true)
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Playlist.Volume = (int)VolumeSlider.Value;
                    }));
                    Thread.Sleep(100); // sleep for 100ms
                }
            };
            backgroundWorker.RunWorkerAsync();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
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
    }
}
