using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Hina;
using Hina.Threading;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net.Extensions.FLV
{
    public static class NetStreamExtensionsFLV
    {
        public static IDisposable WatchAsFlv(this NetStream netStream, PipeWriter writer, bool hasAudio, bool hasVideo)
        {
            var cts = new CancellationTokenSource();
            netStream.WatchAsFlv(writer, hasAudio, hasVideo, cts.Token).Forget();
            return OnDispose.Fire(cts.Cancel);
        }

        public static async Task WatchAsFlv(this NetStream netStream, PipeWriter writer, bool hasAudio, bool hasVideo, CancellationToken cancellation)
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

            object sync = new object();

            flvHeader[4] = (byte)((hasAudio? 4 : 0) | (hasVideo? 1 : 0));

            writer.Write(flvHeader);

            uint lastTimestampValue = 0;
            int lastTagSize = 0;

            BufferBlock<RtmpMessage> writeQueue = new BufferBlock<RtmpMessage>();
            void WriteToFLV(RtmpMessage message)
            {
                var blockAudio = message.ContentType == PacketContentType.Audio && !hasAudio;
                var blockVideo = message.ContentType == PacketContentType.Video && !hasVideo;
                if (!blockAudio && !blockVideo)
                    writeQueue.Post(message);
            }

            netStream.AudioVideoDataReceived += WriteToFLV;

            try
            {
                while (true)
                {
                    var message = await writeQueue.ReceiveAsync(cancellation);

                    if (message == null)
                        break;

                    byte type;
                    IBufferSequence messageData;
                    switch (message)
                    {
                        case AudioData a: type = 8; messageData = a; break;
                        case VideoData v: type = 9; messageData = v; break;
                        default: continue;
                    }

                    var ts = (uint)(message.Timestamp * 0.98);
                    if (ts < lastTimestampValue)
                        continue;

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

                    writer.Write(lastTagSizeBuffer);
                    writer.Write(flvTagHeader);
                    messageData.Write(writer);

                    var result = await writer.FlushAsync(cancellation);

                    if (result.IsCanceled || result.IsCompleted)
                        break;

                    lastTagSize = flvTagHeader.Length + messageData.Length;
                }
            }
            catch (OperationCanceledException) { }

            netStream.AudioVideoDataReceived -= WriteToFLV;

            writer.Complete();
        }
    }
}
