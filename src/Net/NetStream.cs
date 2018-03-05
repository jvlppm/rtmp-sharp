using System.Threading.Tasks;
using RtmpSharp.Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RtmpSharp.Net
{
    public class NetStream
    {
        public IClientDelegate ClientDelegate;

		byte[] flvHeader = {
			(byte) 'F',
			(byte) 'L',
			(byte) 'V',
			0x01, // version
			0x00, // audio/video flags
			0x00,
			0x00,
			0x00,
			0x09
		};

		byte[] flvTagHeader = {
			0x08, // tag data type (8 = audio, 9 = video)
			0, 0, 0, // data size
			0, 0, 0, // timestamp
			0, // timestamp-ex
			0, 0, 0, // streamId - always 0
		};

		byte[] lastTagSizeBuffer = { 0, 0, 0, 0 };

		public IDisposable WatchAsFlv(Action<byte[]> onDataReceived, Func<TimeSpan?> currentTime = null)
		{
			if (onDataReceived == null) throw new ArgumentNullException(nameof(onDataReceived));
			if ( CleanupFlvHandlers.ContainsKey(onDataReceived)) throw new InvalidOperationException("Handler already registered");

			object sync = new object();
			var flvTagTimestamp = new Stopwatch();

			int lastTagSize = 0;

			//flvHeader[4] = hasAudio? 4 : 0 | hasVideo? 1 : 0;
			onDataReceived(flvHeader);

			AudioDataReceived += Handle_AudioData;
			VideoDataReceived += Handle_VideoData;

			CleanupFlvHandlers[onDataReceived] = delegate {
				AudioDataReceived -= Handle_AudioData;
				VideoDataReceived -= Handle_VideoData;
			};

			void Handle_AudioData(byte[] e) => WriteFLVTag(8, e);
			void Handle_VideoData(byte[] e) => WriteFLVTag(9, e);

			void WriteFLVTag(byte type, byte[] e)
			{
				lock (sync)
				{
					var extraFrameTimestamp = currentTime?.Invoke();
					
					lastTagSizeBuffer[0] = (byte)(lastTagSize >> 24);
					lastTagSizeBuffer[1] = (byte)(lastTagSize >> 16);
					lastTagSizeBuffer[2] = (byte)(lastTagSize >> 8);
					lastTagSizeBuffer[3] = (byte)(lastTagSize >> 0);

					flvTagHeader[0] = type;
					flvTagHeader[1] = (byte)(e.Length >> 16);
					flvTagHeader[2] = (byte)(e.Length >> 8);
					flvTagHeader[3] = (byte)(e.Length >> 0);

					var ts = (long)(extraFrameTimestamp != null && extraFrameTimestamp.Value.TotalSeconds > 1? extraFrameTimestamp.Value.TotalMilliseconds : flvTagTimestamp.ElapsedMilliseconds);
					flvTagHeader[4] = (byte)(ts >> 16);
					flvTagHeader[5] = (byte)(ts >> 8);
					flvTagHeader[6] = (byte)(ts >> 0);
					flvTagHeader[7] = (byte)(ts >> 24);

					onDataReceived(lastTagSizeBuffer);
					onDataReceived(flvTagHeader);
					onDataReceived(e);

					lastTagSize = flvTagHeader.Length + e.Length;

					flvTagTimestamp.Start();
				}
			}
			return new OnDispose(delegate {
				Action cleanup;
				if (CleanupFlvHandlers.TryGetValue(onDataReceived, out cleanup) && cleanup != null) {
					cleanup();
					CleanupFlvHandlers.Remove(onDataReceived);
				}
			});
		}

		class OnDispose : IDisposable
		{
			public Action Action { get; }

			public OnDispose(Action action)
			{
				Action = action;
			}

			#region IDisposable Support
			bool disposedValue; // To detect redundant calls

			protected virtual void Dispose(bool disposing)
			{
				if (!disposedValue)
				{
					if (disposing)
					{
						Action();
					}

					// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
					// TODO: set large fields to null.

					disposedValue = true;
				}
			}

			// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
			// ~StopWatchingFlv() {
			//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			//   Dispose(false);
			// }

			// This code added to correctly implement the disposable pattern.
			public void Dispose()
			{
				// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
				Dispose(true);
				// TODO: uncomment the following line if the finalizer is overridden above.
				// GC.SuppressFinalize(this);
			}
			#endregion
		}

        event Action<byte[]> AudioDataReceived;
        event Action<byte[]> VideoDataReceived;

		readonly Dictionary<Action<byte[]>, Action> CleanupFlvHandlers = new Dictionary<Action<byte[]>, Action>();

        public readonly int StreamId;
        readonly RtmpClient client;
        readonly Channel data;
        readonly Channel audio;
        readonly Channel video;

		bool disposed;

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

		public async Task Pause(bool paused)
		{
			var res = await InvokeAsync<object>("pause", paused, 0);
		}

		public async Task Delete()
		{
			await Pause(true);
			//await InvokeAsync<object>("receiveAudio", false);
			//await InvokeAsync<object>("receiveVideo", false);
			//client.DeleteStream(this);
			//disposed = true;
		}

        async Task<T> InvokeAsync<T>(string method, params object[] arguments)
        {
			if (disposed) throw new ObjectDisposedException(nameof(NetStream));
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
                    AudioDataReceived?.Invoke(a.Data);
                    break;
                case VideoData v:
                    VideoDataReceived?.Invoke(v.Data);
                    break;
				default:
					Console.WriteLine($"Received unknown stream data: {e.GetType()}");
					break;
            }
        }
    }
}
