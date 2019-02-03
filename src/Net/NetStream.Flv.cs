using System;
using Hina;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net.Extensions.FLV
{
    public static class NetStreamExtensionsFLV
    {
        public static IDisposable WatchAsFlv(this NetStream netStream, Action<object, byte[]> onDataReceived, bool hasAudio, bool hasVideo)
        {
            byte[] flvHeader = {
                (byte) 'F',
                (byte) 'L',
                (byte) 'V',
                0x01, // version
                0x00, // audio/video flags
                0x00,
                0x00,
                0x00,
                0x09
            };

            byte[] flvTagHeader = {
                0x08, // tag data type (8 = audio, 9 = video)
                0, 0, 0, // data size
                0, 0, 0, // timestamp
                0, // timestamp-ex
                0, 0, 0, // streamId - always 0
            };
            byte[] lastTagSizeBuffer = { 0, 0, 0, 0 };

            if (onDataReceived == null) throw new ArgumentNullException(nameof(onDataReceived));


            object sync = new object();

            flvHeader[4] = (byte)((hasAudio? 4 : 0) | (hasVideo? 1 : 0));
            onDataReceived(netStream, flvHeader);

            uint lastTimestampValue = 0;
            int lastTagSize = 0;

            netStream.AudioVideoDataReceived += WriteToFLV;

            void WriteToFLV(RtmpMessage message)
            {
                byte type;
                IBufferSequence messageData;
                switch (message)
                {
                    case AudioData a: type = 8; messageData = a; break;
                    case VideoData v: type = 9; messageData = v; break;
                    default: return;
                }

                var ts = message.Timestamp;
                if (ts < lastTimestampValue)
                    return;

                lock (sync)
                {
                    lastTimestampValue = ts;

                    lastTagSizeBuffer[0] = (byte)(lastTagSize >> 24);
                    lastTagSizeBuffer[1] = (byte)(lastTagSize >> 16);
                    lastTagSizeBuffer[2] = (byte)(lastTagSize >> 8);
                    lastTagSizeBuffer[3] = (byte)(lastTagSize >> 0);

                    flvTagHeader[0] = type;
                    flvTagHeader[1] = (byte)(messageData.Length >> 16);
                    flvTagHeader[2] = (byte)(messageData.Length >> 8);
                    flvTagHeader[3] = (byte)(messageData.Length >> 0);

                    flvTagHeader[4] = (byte)(ts >> 16);
                    flvTagHeader[5] = (byte)(ts >> 8);
                    flvTagHeader[6] = (byte)(ts >> 0);
                    flvTagHeader[7] = (byte)(ts >> 24);

                    onDataReceived(netStream, lastTagSizeBuffer);
                    onDataReceived(netStream, flvTagHeader);
                    foreach (var buffer in messageData.Read())
                        onDataReceived(netStream, buffer);

                    lastTagSize = flvTagHeader.Length + messageData.Length;
                }
            }
            return new OnDispose(delegate {
                netStream.AudioVideoDataReceived -= WriteToFLV;
            });
        }
    }
}
