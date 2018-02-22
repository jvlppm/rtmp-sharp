namespace RtmpSharp.Net.Messages
{
    abstract class ByteData : RtmpMessage
    {
        public byte[] Data;

        protected ByteData(byte[] data, PacketContentType type) : base(type)
            => Data = data;
    }

    class NotifyMessage : RtmpMessage
    {
        public readonly string Message;
        public readonly object Parameter;

        public NotifyMessage(string message, object parameter) : base(PacketContentType.DataAmf0)
        {
            Message = message;
            Parameter = parameter;
        }
    }

    class AudioData : ByteData
    {
        public AudioData(byte[] data) : base(data, PacketContentType.Audio) { }
    }

    class VideoData : ByteData
    {
        public VideoData(byte[] data) : base(data, PacketContentType.Video) { }
    }
}
