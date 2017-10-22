using System.Collections.Generic;
using RtmpSharp.IO;

namespace RtmpSharp.Net.Messages
{
    partial class SharedObjectMessage : RtmpMessage
    {
        public enum EventType : byte {
            Connect = 1,
            Disconnect = 2,
            ConnectSuccess = 11,

            Status = 7,

            SendMessage = 6,

            SetAttribute = 3,
            UpdateAttribute = 5,
            DeleteAttribute = 10,

            UpdateData = 4,
            ClearData = 8,
            DeleteData = 9,
        }

        public class Event {
            public readonly EventType Type;

            public Event(EventType type) => Type = type;
            public virtual void Encode(ObjectEncoding encoding, AmfWriter writer) { }
        }

        public class ConnectEvent : Event {
            public ConnectEvent() : base(EventType.Connect) { }
        }

        public class ConnectSuccessEvent : Event {
            public ConnectSuccessEvent() : base(EventType.ConnectSuccess) { }
        }

        internal class UpdateDataEvent : Event
        {
            public readonly string Name;
            public readonly object Value;

            public UpdateDataEvent(string name, object value)
                : base(EventType.UpdateData)
            {
                Name = name;
                Value = value;
            }
        }

        public class UnsupportedEvent : Event {
            public Hina.Space<byte> Data;

            public UnsupportedEvent(EventType type, Hina.Space<byte> data) : base(type) {
                Data = data;
            }
        }
    }
}