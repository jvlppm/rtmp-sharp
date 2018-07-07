using RtmpSharp.Net.Messages;
using System;

namespace RtmpSharp.Net
{
    public class Channel
    {
        internal event EventHandler<RtmpMessage> MessageReceived;

        public readonly int ChannelId;
        readonly RtmpClient client;

        internal int ServerChannelId;

        public Channel(RtmpClient client, int channelId)
        {
            ChannelId = channelId;
            this.client = client;
        }

        internal void InternalReceiveMessage(RtmpMessage message)
        {
            MessageReceived?.Invoke(this, message);
        }
    }
}
