using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfDestruct
{
    class DestructionAudioPlayer
    {

        private AudioFileReader audioFileReader;
        private WaveOutEvent outputDevice;

        public DestructionAudioPlayer(string audioFileName, int startAt = 0) {

            Mp3FileReader fileChecker = new Mp3FileReader(audioFileName);

            FileLength = fileChecker.TotalTime.TotalMilliseconds;

            fileChecker.Dispose();
            fileChecker.Close();
            fileChecker = null;

            audioFileReader = new AudioFileReader(audioFileName);
            outputDevice = new WaveOutEvent();

            outputDevice.Init(audioFileReader);

            //TODO: Fix pcm issue
            /*
            if (startAt <= 0)
            {
                WaveOffsetStream offsetStream = new WaveOffsetStream(audioFileReader);
                offsetStream.Skip(startAt);
                outputDevice.Init(offsetStream);
            }
            else
            {
                outputDevice.Init(audioFileReader);
            }
            */
        }

        public void Play() {
            outputDevice.Play();
        }

        public void Stop() {
            outputDevice.Stop();
        }

        public void Close() {
            outputDevice.Dispose();
            audioFileReader.Dispose();
            audioFileReader = null;
            outputDevice = null;
        }

        public double FileLength { get; set; }

    }
}
