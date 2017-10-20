using System.Collections.Generic;
using RtmpSharp.IO;

namespace RtmpSharp.Net.Messages
{
    class SharedObject : RtmpMessage
    {
        static IDictionary<string, SharedObject> SharedObjects = new Dictionary<string, SharedObject>();
        public static SharedObject GetRemote(string name, bool persistence, bool secure)
        {
            SharedObject shared;
            if (SharedObjects.TryGetValue(name, out shared)) {
                return shared;
            }
            shared = new SharedObject(name, persistence, secure);
            shared.events.Add(new ConnectEvent());
            SharedObjects[name] = shared;
            return shared;
        }

        public enum EventType : byte {
            /// <summary>
            /// connect.
            /// </summary>
            ServerConnect = 1,
            /// <summary>
            /// disconnect.
            /// </summary>
            ServerDisconnect = 2,
            /// <summary>
            /// set attribute.
            /// </summary>
            ServerSetAttribute = 3,
            /// <summary>
            /// update data.
            /// </summary>
            ClientUpdateData = 4,
            /// <summary>
            /// update attribute.
            /// </summary>
            ClientUpdateAttribute = 5,
            /// <summary>
            /// send message.
            /// </summary>
            SendMessage = 6,
            /// <summary>
            /// status.
            /// </summary>
            ClientStatus = 7,
            /// <summary>
            /// clear data.
            /// </summary>
            ClientClearData = 8,
            /// <summary>
            /// delete data.
            /// </summary>
            ClientDeleteData = 9,
            /// <summary>
            /// delete attribute.
            /// </summary>
            DeleteAttribute = 10,
            /// <summary>
            /// initial data.
            /// </summary>
            ClientInitialData = 11
        }

        public class Event {
            public readonly EventType Type;

            public Event(EventType type) => Type = type;
            public virtual void Encode(ObjectEncoding encoding, AmfWriter writer) { }
        }

        public class ConnectEvent : Event {
            public ConnectEvent() : base(EventType.ServerConnect) { }
        }

        public readonly string Name;
        public int Version { get; private set; } = 1;

        public bool Persistence { get; private set; }
        public bool Secure { get; private set; }

        public List<Event> events = new List<Event>();

        SharedObject(string name, bool persistence, bool secure)
            : base(PacketContentType.SharedObjectAmf0)
        {
            Name = name;
            Secure = secure;
            Persistence = persistence;
        }
    }
}