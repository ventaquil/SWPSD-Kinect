using CSCore;
using CSCore.Codecs;
using CSCore.CoreAudioAPI;
using CSCore.SoundOut;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kinect
{
    public class MusicPlayer
    {
        private ISoundOut SoundOut;

        private IWaveSource WaveSource;

        public event EventHandler<PlaybackStoppedEventArgs> PlaybackStopped;

        public PlaybackState PlaybackState
        {
            get
            {
                if (SoundOut != null)
                {
                    return SoundOut.PlaybackState;
                }

                return PlaybackState.Stopped;
            }
        }

        public TimeSpan Position
        {
            get
            {
                if (WaveSource != null)
                {
                    return WaveSource.GetPosition();
                }

                return TimeSpan.Zero;
            }
            set
            {
                if (WaveSource != null)
                {
                    WaveSource.SetPosition(value);
                }
            }
        }

        public TimeSpan Length
        {
            get
            {
                if (WaveSource != null)
                {
                    return WaveSource.GetLength();
                }

                return TimeSpan.Zero;
            }
        }

        public int Volume
        {
            get
            {
                if (SoundOut != null)
                {
                    return Math.Min(100, Math.Max((int)(SoundOut.Volume * 100), 0));
                }

                return 100;
            }
            set
            {
                if (SoundOut != null)
                {
                    SoundOut.Volume = Math.Min(1.0f, Math.Max(value / 100f, 0f));
                }
            }
        }

        public void Open(string filename, MMDevice device)
        {
            CleanupPlayback();

            WaveSource = CodecFactory.Instance.GetCodec(filename)
                .ToSampleSource()
                .ToMono()
                .ToWaveSource();

            SoundOut = new WasapiOut() { Latency = 100, Device = device };
            SoundOut.Initialize(WaveSource);

            if (PlaybackStopped != null)
            {
                SoundOut.Stopped += PlaybackStopped;
            }
        }

        public void Play()
        {
            if (SoundOut != null)
            {
                SoundOut.Play();
            }
        }

        public void Pause()
        {
            if (SoundOut != null)
            {
                SoundOut.Pause();
            }
        }

        public void Stop()
        {
            if (SoundOut != null)
            {
                SoundOut.Stop();
            }
        }

        private void CleanupPlayback()
        {
            if (SoundOut != null)
            {
                SoundOut.Dispose();
                SoundOut = null;
            }

            if (WaveSource != null)
            {
                WaveSource.Dispose();
                WaveSource = null;
            }
        }
    }
}
