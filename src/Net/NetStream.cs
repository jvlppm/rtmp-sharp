using System.Threading.Tasks;
using RtmpSharp.Net.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Hina.Threading;

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
        readonly byte[] lastTagSizeBuffer = { 0, 0, 0, 0 };

		public IDisposable WatchAsFlv(Action<object, byte[]> onDataReceived, Func<TimeSpan?> currentTime = null, bool hasAudio = false, bool hasVideo = false)
		{
			if (onDataReceived == null) throw new ArgumentNullException(nameof(onDataReceived));
			if ( CleanupFlvHandlers.ContainsKey(onDataReceived)) throw new InvalidOperationException("Handler already registered");

			object sync = new object();
			var flvTagTimestamp = new Stopwatch();

			int lastTagSize = 0;

            //flvHeader[4] = (byte)((hasAudio? 4 : 0) | (hasVideo? 1 : 0));
			onDataReceived(this, flvHeader);

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

					onDataReceived(this, lastTagSizeBuffer);
					onDataReceived(this, flvTagHeader);
					onDataReceived(this, e);

					lastTagSize = flvTagHeader.Length + e.Length;

					flvTagTimestamp.Start();
				}
			}
			return new OnDispose(delegate {
                if (CleanupFlvHandlers.TryGetValue(onDataReceived, out Action cleanup) && cleanup != null)
                {
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

		readonly Dictionary<Action<object, byte[]>, Action> CleanupFlvHandlers = new Dictionary<Action<object, byte[]>, Action>();

        public readonly int StreamId;
        readonly RtmpClient client;
        public readonly Channel data;
        public readonly Channel audio;
        public readonly Channel video;

        internal int ServerStreamId;
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

        public async Task Play(string videoId)
        {
            client.RegisteringStream = this;
            await InvokeAsync<object>(data.ChannelId, "play", videoId);
        }

		public async Task Pause(bool paused)
		{
            var res = await InvokeAsync<object>(data.ChannelId, "pause", paused, 0);
		}

		public async Task Delete()
		{
            if (ServerStreamId > 0) {
    			InvokeAsync<object>((int)ServerStreamId, "closeStream").Forget();
                await client.InvokeAsync<object>("deleteStream", (int)ServerStreamId);
                client.DeleteStream(this);
            }
			disposed = true;
		}

        async Task<T> InvokeAsync<T>(int channelId, string method, params object[] arguments)
        {
			if (disposed) throw new ObjectDisposedException(nameof(NetStream));
            var command = new InvokeAmf0 {
                MethodName = method,
                Arguments = arguments,
                InvokeId = client.NextInvokeId()
            };
            var result = await client.InternalCallAsync(command, channelId);
            return NanoTypeConverter.ConvertTo<T>(result);
        }

        internal void Data_MessageReceived(object sender, RtmpMessage e)
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
