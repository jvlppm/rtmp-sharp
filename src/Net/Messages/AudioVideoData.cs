using System;
using System.Buffers;
using System.IO.Pipelines;
using RtmpSharp.IO;

namespace RtmpSharp.Net.Messages
{
    public enum SoundFormat : byte
    {
        LinearPCM_PlatformEndian = 0,
        ADPCM = 1,
        MP3 = 2,
        LinearPCM_LittleEndian = 3,
        Nellymoser_16kHzMono = 4,
        Nellymoser_8kHzMono = 5,
        Nellymoser = 6,
        G711_aLawLogarithmicPCM = 7,
        G711_muLawLogarithmicPCM = 8,
        AAC = 10,
        Speex = 11,
        MP3_8kHz = 14,
        DeviceSpecificSound = 15
    }

    public enum SoundRate
    {
        kHz_5_5 = 0,
        kHz_11 = 1,
        kHz_22 = 2,
        kHz_44 = 3
    }

    public enum SoundSize
    {
        Sound_8bit = 0,
        Sound_16bit = 1
    }

    public enum SoundType
    {
        Mono = 0,
        Stereo = 1
    }

    public struct SoundProperties
    {
        public SoundFormat format;
        public SoundRate rate;
        public SoundSize size;
        public SoundType type;

        public SoundProperties(SoundFormat format, SoundRate rate, SoundSize size, SoundType type)
        {
            this.format = format;
            this.rate = rate;
            this.size = size;
            this.type = type;
        }

        internal static SoundProperties FromHeader(byte firstByte)
        {
            var soundFormat = (SoundFormat)(firstByte >> 4);
            var soundRate = (SoundRate)((firstByte >> 2) & 0b11);
            var soundSize = (SoundSize)((firstByte >> 1) & 0b1);
            var soundType = (SoundType)(firstByte & 0b1);
            return new SoundProperties(soundFormat, soundRate, soundSize, soundType);
        }

        internal byte GetHeader()
        {
            var firstByte = (byte)0;
            firstByte |= (byte)(((byte)format & 0b1111) << 4);
            firstByte |= (byte)(((byte)rate & 0b11) << 2);
            firstByte |= (byte)(((byte)size & 0b1) << 1);
            firstByte |= (byte)((byte)type & 0b1);
            return firstByte;
        }
    }

    interface IBufferSequence
    {
        int Length { get; }
        void Write(PipeWriter writer);
    }

    public delegate void SoundDataHandler(SoundProperties soundProperties, byte[] soundData);

    public interface IAudioSource : IDisposable
    {
        event SoundDataHandler OnSound;
    }

    public interface IAudioDevice
    {
        IAudioSource OpenAudioSource();
    }

    abstract class ByteData : RtmpMessage, IBufferSequence
    {
        public byte[] Data;

        public int Length => Data.Length;

        public void Write(PipeWriter writer)
        {
            writer.Write(Data);
        }

        protected ByteData(byte[] data, PacketContentType type) : base(type)
            => Data = data;
    }

    class AudioData : RtmpMessage, IBufferSequence
    {
        private readonly SoundProperties properties;
        private readonly byte[] soundData;
        private readonly bool hasData;

        public int Length => hasData? soundData.Length + 1 : 0;

        public AudioData(SoundProperties properties, byte[] soundData)
             : base(PacketContentType.Audio)
        {
            this.properties = properties;
            this.soundData = soundData;
            hasData = true;
        }

        internal static RtmpMessage Read(AmfReader r)
        {
            if (r.Remaining < 1) return default(AudioData);

            var soundInfo = SoundProperties.FromHeader(r.ReadByte());
            var soundData = r.ReadBytes(r.Remaining);

            return new AudioData(soundInfo, soundData);
        }

        internal void Write(AmfWriter w)
        {
            if (!hasData) return;
            byte firstByte = properties.GetHeader();
            w.WriteByte(firstByte);
            w.WriteBytes(soundData);
        }

        public void Write(PipeWriter writer)
        {
            var size = Length;
            if (size <= 0) return;
            var memory = writer.GetMemory(size).Span;
            memory[0] = properties.GetHeader();
            soundData.CopyTo(memory.Slice(1));
            writer.Advance(size);
        }
    }

    class VideoData : ByteData
    {
        public VideoData(byte[] data) : base(data, PacketContentType.Video) { }
    }
}
