// field is never assigned to, and will always have its default value null
#pragma warning disable CS0649

namespace RtmpSharp.Net.Messages
{
    class Notify : RtmpMessage
    {
        public readonly string Action;
        public readonly object[] Arguments;

        protected Notify(PacketContentType type, string action, object[] arguments) : base(type)
        {
            Action = action;
            Arguments = arguments;
        }
    }

    class NotifyAmf0 : Notify
    {
        public NotifyAmf0(string action, params object[] arguments)
            : base(PacketContentType.DataAmf0, action, arguments) { }
    }

    class NotifyAmf3 : Notify
    {
        public NotifyAmf3(string action, params object[] arguments)
            : base(PacketContentType.DataAmf3, action, arguments) { }
    }
}
