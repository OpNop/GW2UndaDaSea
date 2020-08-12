using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly List<WavePlayer> _songs = new List<WavePlayer>();
        private MixingWaveProvider32 _mixer;
        private WaveOutEvent _outputDevice;

        public MainWindow()
        {
            //Load songs
            _songs.Add(new WavePlayer(new MemoryStream(Properties.Resources.uts_loop4)));
            _songs.Add(new WavePlayer(new MemoryStream(Properties.Resources.pus_loop)));

            //Set volumes
            _songs.ForEach(s => s.Channel.Volume = 0);

            //Sir Mix-a-lot
            _mixer = new MixingWaveProvider32(_songs.Select(c => c.Channel));

            //Setup output
            _outputDevice = new WaveOutEvent();
            _outputDevice.Init(_mixer);
            _outputDevice.Volume = 1;
            _outputDevice.Play();

            InitializeComponent();
            Topmost = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Do da mumble
            mumbleLoopThread = new Thread(MumbleLoop);
            mumbleLoopThread.Start();
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
                        Dispatcher.Invoke(new Action<IGw2MumbleClient>(m =>
                        {
                            //Adjust volume acording to elevation
                            float Yloc = Convert.ToSingle(m.AvatarPosition.Y);

                            //Above sea level
                            if(Yloc > 0)
                            {
                                _songs[0].Channel.Volume = Map(Yloc, 0, 3, 0.01f, 0f);
                                _songs[1].Channel.Volume = 0;
                            }
                            //Unda Da Sea!!!!
                            else if(Yloc <= 0 && Yloc > -140)
                            {
                                _songs[0].Channel.Volume = Map(Yloc, -30, 0, 1, 0.01f);
                                _songs[1].Channel.Volume = 0;
                            }
                            //Getting deep
                            else if(Yloc <= -130 && Yloc > -150)
                            {
                                _songs[0].Channel.Volume = Map(Yloc, -130, -150, 0.99f, 0f);
                                _songs[1].Channel.Volume = Map(Yloc, -150, -130, 0.1f, 0f);
                            }
                            //TOO DEEP
                            else if (Yloc <= -150)
                            {
                                _songs[1].Channel.Volume = Map(Yloc, -170, -150, 1f, 0.1f);
                            }

                            DepthLabel.Content = $"Depth: {(Yloc+1f):n2} Meters";
                            DepthSlider.Value = Clamp((Yloc * -1), 0, 1000);

                        }), client.Mumble);
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
            _songs.Clear();

        }

        private static float Map(float value, float fromLow, float fromHigh, float toLow, float toHigh)
        {
            var result = (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
            //Lets not get crazy here, keep it between 0 and 1
            return Clamp(result, 0, 1);
        }

        public static float Clamp(float value, float min, float max)
        {
            return (value < min) ? min : (value > max) ? max : value;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            stopRequested = true;
        }

        private void MasterVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _outputDevice.Volume = (float)MasterVolume.Value / 100f;
        }
    }
}
