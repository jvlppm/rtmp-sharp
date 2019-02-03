using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RtmpSharp.Net
{
    partial class RtmpClient
    {
        readonly IDictionary<int, Net.ChunkStream> ReceivingChunks = new Dictionary<int, Net.ChunkStream>();

        TaskCompletionSource<int> currentStreamInitialization;
        Task<IDisposable> streamInitialization = Task.FromResult<IDisposable>(null);

        public async Task<NetStream> CreateStreamAsync()
        {
            var streamId = await InvokeAsync<int>("createStream");
            var data = new Net.ChunkStream(streamId * 5 - 1);
            var video = new Net.ChunkStream(streamId * 5);
            var audio = new Net.ChunkStream(streamId * 5 + 1);

            return new NetStream(this, streamId, data, video, audio);
        }

        internal Task<IDisposable> PrepareStreamForReceivingData(NetStream netStream, Action p)
        {
            return streamInitialization = streamInitialization.ContinueWith(t => DoPrepareStreamForReceivingData(netStream, p)).Unwrap();
        }

        async Task<IDisposable> DoPrepareStreamForReceivingData(NetStream netStream, Action p)
        {
            currentStreamInitialization = new TaskCompletionSource<int>();
            var register = currentStreamInitialization.Task.ContinueWith(t =>
            {
                var serverStreamId = t.Result;
                return RegisterStreamForReceivingData(netStream, serverStreamId);
            }, TaskContinuationOptions.ExecuteSynchronously);

            p();
            return await register;
        }

        IDisposable RegisterStreamForReceivingData(NetStream stream, int streamId)
        {
            var dataChunkId = streamId * 5 - 1;
            var videoChunkId = streamId * 5;
            var audioChunkId = streamId * 5 + 1;

            if (ReceivingChunks.ContainsKey(dataChunkId) ||
                ReceivingChunks.ContainsKey(videoChunkId) ||
                ReceivingChunks.ContainsKey(audioChunkId))
                throw new InvalidOperationException("Stream ID conflicts with already registered stream");

            ReceivingChunks[dataChunkId] = stream.data;
            ReceivingChunks[videoChunkId] = stream.video;
            ReceivingChunks[audioChunkId] = stream.audio;
            return Hina.OnDispose.Fire(delegate
            {
                ReceivingChunks.Remove(dataChunkId);
                ReceivingChunks.Remove(videoChunkId);
                ReceivingChunks.Remove(audioChunkId);
            });
        }
    }
}
