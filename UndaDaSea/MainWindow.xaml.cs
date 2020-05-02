using System;
using System.IO;
using System.Threading;
using System.Windows;
using Gw2Sharp;
using Gw2Sharp.Mumble;
using NAudio.Wave;

namespace UndaDaSea
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private readonly Gw2Client client = new Gw2Client();
        private Thread? mumbleLoopThread;
        private bool stopRequested;

        //Audio Player stuff
        private StreamMediaFoundationReader _audioFile;
        private WaveOutEvent _outputDevice;
        private LoopStream _loop;

        public MainWindow()
        {
            InitializeComponent();
            Topmost = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mumbleLoopThread = new Thread(MumbleLoop);
            mumbleLoopThread.Start();

            //Load and play
            _audioFile = new StreamMediaFoundationReader(new MemoryStream(Properties.Resources.uts_loop4));
            _loop = new LoopStream(_audioFile);
            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_loop);
            _outputDevice.Volume = 0;
            _outputDevice.Play();
        }

        private void MumbleLoop()
        {
            do
            {
                bool shouldRun = true;
                client.Mumble.Update();
                if (!client.Mumble.IsAvailable)
                    shouldRun = false;

                int mapId = client.Mumble.MapId;
                if (mapId == 0)
                    shouldRun = false;

                if (shouldRun)
                {
                    try
                    {
                        this.Dispatcher.Invoke(new Action<IGw2MumbleClient>(m =>
                        {
                            //Adjust volume acording to elevation
                            float volume = 0;
                            float Yloc = Convert.ToSingle(m.AvatarPosition.Y);
                            if (Yloc <= 0)
                            {
                                volume = map(Yloc, -30, 0, Convert.ToSingle(MaxVolume.Value), 0.01f);
                            }
                            else
                            {
                                volume = map(Yloc, 0, 3, 0.01f, 0f);
                            }

                            //Lets not get crazy here, keep it between 0 and 1
                            volume = Clamp(volume, 0, 1);

                            _outputDevice.Volume = volume;
                            volumeSlider.Value = volume;
#if DEBUG
                            DepthLabel.Content = $"Depth ({_outputDevice.Volume})";
#endif
                        }), this.client.Mumble);
                    }
                    catch (ObjectDisposedException)
                    {
                        // The application is likely closing
                        break;
                    }
                }

                Thread.Sleep(1000 / 60);
            } while (!stopRequested);

            _outputDevice.Stop();
            _outputDevice.Dispose();
            _outputDevice = null;
            _audioFile.Dispose();
            _audioFile = null;
            _loop.Dispose();
            _loop = null;

        }

        private static float map(float value, float fromLow, float fromHigh, float toLow, float toHigh)
        {
            return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
        }
        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopRequested = true;
        }
    }
}
