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

        public class UpdateDataEvent : Event
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

		public class DeleteDataEvent : Event
		{
			public readonly string Name;

			public DeleteDataEvent(string name)
				: base(EventType.DeleteData)
			{
				Name = name;
			}
		}

        public class ClearDataEvent : Event {
            public ClearDataEvent() : base(EventType.ClearData) { }
        }

        public class SendMessageEvent : Event
        {
            public readonly string Name;
            public readonly object[] Parameters;

            public SendMessageEvent(string name, object[] parameters)
                : base(EventType.SendMessage)
            {
                Name = name;
                Parameters = parameters;
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