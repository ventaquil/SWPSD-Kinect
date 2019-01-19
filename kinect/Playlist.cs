using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace Kinect
{
    public class Playlist
    {
        public int CurrentTrack { get; private set; } = 0;

        private MMDevice MMDevice;

        private bool IsPaused = false;

        public bool IsPlaying
        {
            get;
            private set;
        } = false;

        private bool IsPlayingInLoop = false;

        private MusicPlayer MusicPlayer = new MusicPlayer();

        public event EventHandler<int> TrackChanged;

        private List<Track> Tracks = new List<Track>();

        public event EventHandler<PlaybackStoppedEventArgs> MusicStopped
        {
            add
            {
                MusicPlayer.PlaybackStopped += value;
            }
            remove
            {
                MusicPlayer.PlaybackStopped -= value;
            }
        }

        public int Length
        {
            get => (int)MusicPlayer.Length.TotalMilliseconds / 100;
        }

        public int Position
        {
            get => (int)MusicPlayer.Position.TotalMilliseconds / 100;
            set => MusicPlayer.Position = TimeSpan.FromMilliseconds(value * 100);
        }

        public Playlist() : this(GetDefaultMMDevice())
        {
        }

        public Playlist(MMDevice mmDevice)
        {
            MMDevice = mmDevice;

            MusicPlayer.PlaybackStopped += (object sender, PlaybackStoppedEventArgs e) =>
            {
                if (IsPlayingInLoop && (Position == Length))
                {
                    NextTrack();

                    PlayInLoop();
                }
            };
        }

        private static MMDevice GetDefaultMMDevice()
        {
            using (MMDeviceEnumerator mmDeviceEnumerator = new MMDeviceEnumerator())
            {
                return mmDeviceEnumerator.EnumAudioEndpoints(DataFlow.Render, DeviceState.Active).First();
            }
        }

        public void Add(Track track)
        {
            if (!Tracks.Contains(track))
            {
                Tracks.Add(track);
            }
        }

        public Track[] GetTracks()
        {
            return Tracks.ToArray();
        }

        private void PlayCurrentTrack()
        {
            if (IsPaused)
            {
                IsPaused = false;
            }
            else
            {
                LoadTrack();
            }

            MusicPlayer.Play();
        }

        internal void LoadTrack()
        {
            Track track = Tracks.Skip(CurrentTrack).First();

            LoadTrack(track);
        }

        private void LoadTrack(Track track)
        {
            MusicPlayer.Open(track.Path, MMDevice);
        }

        private void NextTrack()
        {
            ++CurrentTrack;
            CurrentTrack %= Tracks.Count;

            TrackChanged?.Invoke(this, CurrentTrack);
        }

        internal void PlayOnce()
        {
            if ((Tracks.Count > 0) && !IsPlaying)
            {
                IsPlaying = true;

                PlayCurrentTrack();

                IsPlaying = false;
            }
        }

        internal void Play()
        {
            IsPlayingInLoop = true;

            PlayInLoop();
        }

        private void PlayInLoop()
        {
            if (IsPlayingInLoop == true)
            {
                PlayOnce();
            }
        }

        internal void Pause()
        {
            if (MusicPlayer.PlaybackState != PlaybackState.Stopped)
            {
                IsPaused = true;

                MusicPlayer.Pause();
            }
        }

        private void StopTrack()
        {
            IsPaused = false;

            IsPlayingInLoop = false;

            MusicPlayer.Stop();
            MusicPlayer.Position = TimeSpan.FromMilliseconds(0.0);
        }

        internal void Stop()
        {
            StopTrack();
        }

        private void PreviousTrack()
        {
            CurrentTrack = (Tracks.Count + --CurrentTrack) % Tracks.Count;

            TrackChanged?.Invoke(this, CurrentTrack);
        }

        public void Next()
        {
            StopTrack();

            NextTrack();

            Play();
        }

        public void Previous()
        {
            StopTrack();

            PreviousTrack();

            Play();
        }
    }
}
