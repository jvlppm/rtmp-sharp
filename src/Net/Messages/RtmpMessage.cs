﻿namespace RtmpSharp.Net.Messages
{
    abstract class RtmpMessage
    {
        public PacketContentType ContentType;

        protected RtmpMessage(PacketContentType contentType) => ContentType = contentType;

        public uint Timestamp { get; internal set; }
    }
}
