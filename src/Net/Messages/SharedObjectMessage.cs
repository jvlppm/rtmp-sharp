using System.Collections.Generic;
using RtmpSharp.IO;

namespace RtmpSharp.Net.Messages
{
    partial class SharedObjectMessage : RtmpMessage
    {
        public readonly string Name;

        public int Version { get; set; } = 1;
        public bool Persistent { get; set; }

        public List<Event> Events = new List<Event>();

        public SharedObjectMessage(string name, bool persistent)
            : base(PacketContentType.SharedObjectAmf0)
        {
            Name = name;
            Persistent = persistent;
        }
    }
}