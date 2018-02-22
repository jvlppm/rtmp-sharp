using System.Threading.Tasks;
using RtmpSharp.Net.Messages;
using System;

namespace RtmpSharp.Net
{
    public class NetStream
    {
        public IClientDelegate ClientDelegate;

        public event EventHandler<byte[]> AudioDataReceived;
        public event EventHandler<byte[]> VideoDataReceived;

        public readonly int StreamId;
        readonly RtmpClient client;
        readonly Channel data;
        readonly Channel audio;
        readonly Channel video;

        internal NetStream(RtmpClient client, int streamId, Channel data, Channel video, Channel audio)
        {
            StreamId = streamId;
            this.client = client;
            this.data = data;
            this.audio = audio;
            this.video = video;

            this.data.MessageReceived += Data_MessageReceived;
            this.audio.MessageReceived += Data_MessageReceived;
            this.video.MessageReceived += Data_MessageReceived;
        }

        public async Task<object> Play(string videoId)
        {
            var res = await InvokeAsync<object>("play", videoId);
            return res;
        }

        async Task<T> InvokeAsync<T>(string method, params object[] arguments)
        {
            var command = new InvokeAmf0 {
                MethodName = method,
                Arguments = arguments,
                InvokeId = client.NextInvokeId()
            };
            var result = await client.InternalCallAsync(command, data.ChannelId);
            return NanoTypeConverter.ConvertTo<T>(result);
        }

        void Data_MessageReceived(object sender, RtmpMessage e)
        {
            switch (e)
            {
                case NotifyMessage n:
                    ClientDelegate?.Invoke(n.Message, new[] { n.Parameter });
                    break;
                case Invoke i:
                    ClientDelegate?.Invoke(i.MethodName, i.Arguments);
                    break;
                case AudioData a:
                    AudioDataReceived?.Invoke(this, a.Data);
                    break;
                case VideoData v:
                    VideoDataReceived?.Invoke(this, v.Data);
                    break;
            }
        }
    }
}
