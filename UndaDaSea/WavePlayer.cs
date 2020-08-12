using NAudio.Wave;
using System.IO;

namespace UndaDaSea
{
    public class WavePlayer
    {
        StreamMediaFoundationReader Reader;
        public WaveChannel32 Channel { get; set; }

        public WavePlayer(Stream Stream)
        {
            Reader = new StreamMediaFoundationReader(Stream);
            var loop = new LoopStream(Reader);
            Channel = new WaveChannel32(loop) { PadWithZeroes = false };
        }

        public void Dispose()
        {
            if (Channel != null)
            {
                Channel.Dispose();
                Reader.Dispose();
            }
        }

    }
}
