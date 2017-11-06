using System;
namespace RtmpSharp.Net.Messages
{
	class UnknownData : ByteData
	{
		public UnknownData(byte[] data) : base(data, PacketContentType.Unknown) { }
	}
}
