using common.extends;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NSpeex;
using ozeki;
using System;
using System.Collections.Generic;

namespace audio.test
{

    class Program
    {
        static void Main(string[] args)
        {
            SpeexPreprocessor sp = new SpeexPreprocessor(320);
            sp.SetNoiseReductionLevel(NoiseReductionLevel.Medium);
            sp.AGC = true;
            sp.AGCMaxGain = 300;
            sp.AGCSpeed = 15;

            SpeexEncoder encoder = new SpeexEncoder(BandMode.Narrow);
            WaveFormat waveFormat = new WaveFormat(encoder.FrameSize * 50, 16, 1);

            JitterBufferWaveProvider waveProvider = new JitterBufferWaveProvider();
            WasapiOut waveOut = new WasapiOut(AudioClientShareMode.Shared, 20);
            waveOut.Init(waveProvider);
            waveOut.Volume = 0.5f;
            waveOut.Play();

            WaveInEvent waveIn = new WaveInEvent { WaveFormat = waveFormat, BufferMilliseconds = 40, NumberOfBuffers = 2 };
            waveIn.DataAvailable += (s, e) =>
            {
                short[] buffer = sp.Filter(e.Buffer).ToShortArray();
                byte[] encodedData = new byte[e.BytesRecorded];
                int encodedBytes = encoder.Encode(buffer, 0, buffer.Length, encodedData, 0, encodedData.Length);
                if (encodedBytes != 0)
                {
                    byte[] upstreamFrame = new byte[encodedBytes];
                    Buffer.BlockCopy(encodedData, 0, upstreamFrame, 0, encodedBytes);

                    waveProvider.Write(upstreamFrame, 0, upstreamFrame.Length);
                }

            };
            waveIn.StartRecording();

            Console.ReadLine();
        }
    }



    public class JitterBufferWaveProvider : WaveStream
    {
        private readonly SpeexDecoder decoder = new SpeexDecoder(BandMode.Narrow);
        private readonly SpeexJitterBuffer jitterBuffer;

        private readonly WaveFormat waveFormat;
        private readonly object readWriteLock = new object();

        public JitterBufferWaveProvider()
        {
            waveFormat = new WaveFormat(decoder.FrameSize * 50, 16, 1);
            jitterBuffer = new SpeexJitterBuffer(decoder);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int peakVolume = 0;
            int bytesRead = 0;
            lock (readWriteLock)
            {
                while (bytesRead < count)
                {
                    if (exceedingBytes.Count != 0)
                    {
                        buffer[bytesRead++] = exceedingBytes.Dequeue();
                    }
                    else
                    {
                        short[] decodedBuffer = new short[decoder.FrameSize * 2];
                        jitterBuffer.Get(decodedBuffer);
                        for (int i = 0; i < decodedBuffer.Length; ++i)
                        {
                            if (bytesRead < count)
                            {
                                short currentSample = decodedBuffer[i];
                                peakVolume = currentSample > peakVolume ? currentSample : peakVolume;
                                currentSample.GetBytes().CopyTo(buffer, offset + bytesRead);
                                bytesRead += 2;
                            }
                            else
                            {
                                var bytes = decodedBuffer[i].GetBytes();
                                exceedingBytes.Enqueue(bytes[0]);
                                exceedingBytes.Enqueue(bytes[1]);
                            }
                        }
                    }
                }
            }

            OnVolumeUpdated(peakVolume);

            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            lock (readWriteLock)
            {
                jitterBuffer.Put(buffer);
            }
        }

        public override long Length
        {
            get { return 1; }
        }

        public override long Position
        {
            get { return 0; }
            set { throw new NotImplementedException(); }
        }

        public override WaveFormat WaveFormat
        {
            get
            {
                return waveFormat;
            }
        }

        public EventHandler<VolumeUpdatedEventArgs> VolumeUpdated;

        private void OnVolumeUpdated(int volume)
        {
            var eventHandler = VolumeUpdated;
            if (eventHandler != null)
            {
                eventHandler.BeginInvoke(this, new VolumeUpdatedEventArgs { Volume = volume }, null, null);
            }
        }

        private readonly Queue<byte> exceedingBytes = new Queue<byte>();
    }

    public class VolumeUpdatedEventArgs : EventArgs
    {
        public int Volume { get; set; }
    }
}
