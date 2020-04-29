using System;
using System.Threading;
using System.Windows;
using Gw2Sharp;
using Gw2Sharp.Mumble;

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

        public MainWindow()
        {
            InitializeComponent();
            Topmost = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mumbleLoopThread = new Thread(MumbleLoop);
            mumbleLoopThread.Start();
            underTheSea.Play();
        }

        private void underTheSea_MediaEnded(object sender, RoutedEventArgs e)
        {
            underTheSea.Position = TimeSpan.Zero;
        }

        private void MumbleLoop()
        {
            do
            {
                bool shouldRun = true;
                client.Mumble.Update();
                if (!client.Mumble.IsAvailable)
                    shouldRun = false;

                int mapId = this.client.Mumble.MapId;
                if (mapId == 0)
                    shouldRun = false;

                if (shouldRun)
                {

                    try
                    {
                        this.Dispatcher.Invoke(new Action<IGw2MumbleClient>(m =>
                        {
                            //Adjust volume acording to elevation
                            double volume = 0;
                            double Yloc = m.AvatarPosition.Y;
                            //double maxVol = MaxVolume.Value;
                            if (Yloc <= 0)
                            {
                                volume = map(Yloc, -30, 0, MaxVolume.Value, 0.01f);
                                //wplayer.settings.volume = volume;
                            }
                            else
                            {
                                volume = map(Yloc, 0, 3, 0.01f, 0f);
                                //wplayer.settings.volume = Math.Max(0, volume);
                            }

                            underTheSea.Volume = volume;
                            volumeSlider.Value = volume;

                        }), this.client.Mumble);
                    }
                    catch (ObjectDisposedException)
                    {
                        // The application is likely closing
                        break;
                    }
                }

                Thread.Sleep(1000 / 60);
            } while (!this.stopRequested);
        }

        private static double map(double value, double fromLow, double fromHigh, double toLow, double toHigh)
        {
            return (value - fromLow) * (toHigh - toLow) / (fromHigh - fromLow) + toLow;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.stopRequested = true;
        }
    }
}
