using System;
using System.Threading.Tasks;
using Hina;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net
{
    public class ChunkStream
    {
        internal event EventHandler<RtmpMessage> MessageReceived;

        public readonly int Id;

        public ChunkStream(int id) => Id = id;

        internal void InternalReceiveMessage(RtmpMessage message)
            => MessageReceived?.Invoke(this, message);
    }

    public class NetStream : IDisposable
    {
        public readonly RtmpClient Client;

        public event EventHandler Disposed;

        internal readonly int Id;

        internal readonly ChunkStream data;
        internal readonly ChunkStream audio;
        internal readonly ChunkStream video;

        public IClientDelegate ClientDelegate;

        internal event Action<RtmpMessage> AudioVideoDataReceived;

        bool disposedValue;
        IDisposable receiveDataRegistration;

        internal NetStream(RtmpClient client, int id, ChunkStream data, ChunkStream video, ChunkStream audio)
        {
            Id = id;

            Client = client;

            this.data = data;
            this.video = video;
            this.audio = audio;

            data.MessageReceived += Data_MessageReceived;
            video.MessageReceived += Data_MessageReceived;
            audio.MessageReceived += Data_MessageReceived;
        }

        public async Task Play(string videoId)
        {
            receiveDataRegistration?.Dispose();
            receiveDataRegistration = await Client.PrepareStreamForReceivingData(this, () => Invoke("play", videoId));
        }

        public void Publish(PublishType type, string videoId)
        {
            string mode = type.ToString().ToLowerInvariant();
            Invoke("publish", videoId, mode);
        }

        public void Pause(bool paused)
		{
            Invoke("pause", paused, 0);
		}

        public void Delete()
		{
            if (!disposedValue)
            {
                Disposed?.Invoke(this, EventArgs.Empty);
                Invoke("deleteStream", Id);
                receiveDataRegistration?.Dispose();
                disposedValue = true;
            }
		}

        public IDisposable RedirectAsync(NetStream stream)
        {
            AudioVideoDataReceived += WriteToFLV;

            void WriteToFLV(RtmpMessage message)
            {
                stream.Send(message);
            }

            return new OnDispose(delegate {
                AudioVideoDataReceived -= WriteToFLV;
            });
        }

        void Invoke(string method, params object[] arguments)
        {
            Send(new InvokeAmf0
            {
                MethodName = method,
                Arguments = arguments,
                InvokeId = 0
            });
        }

        public IDisposable AttachAudio(IAudioDevice service)
        {
            void onSound(SoundProperties properties, byte[] data)
                => Send(new AudioData(properties, data));

            var source = service.OpenAudioSource();

            source.OnSound += onSound;
            return OnDispose.Fire(delegate {
                source.OnSound -= onSound;
                source.Dispose();
            });
        }

        internal void Send(RtmpMessage message)
        {
            if (disposedValue) throw new ObjectDisposedException(nameof(NetStream));
            switch (message)
            {
                case AudioData a:
                    Client.InternalSend(message, audio.Id, Id);
                    break;
                case VideoData v:
                    Client.InternalSend(message, video.Id, Id);
                    break;
                default:
                    Client.InternalSend(message, data.Id, Id);
                    break;
            }
        }

        internal void Data_MessageReceived(object sender, RtmpMessage e)
        {
            switch (e)
            {
                case Notify n:
                    ClientDelegate?.Invoke(n.Action, n.Arguments);
                    break;
                case Invoke i:
                    ClientDelegate?.Invoke(i.MethodName, i.Arguments);
                    break;
                case AudioData a:
                    if (a.Length > 0)
                        AudioVideoDataReceived?.Invoke(e);
                    break;
                case VideoData v:
                    if (v.Length > 0)
                        AudioVideoDataReceived?.Invoke(e);
                    break;
				default:
					Console.WriteLine($"Received unknown stream data: {e.GetType()}");
					break;
            }
        }

        public enum PublishType
        {
            Live,
            Record,
            Append,
        }

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Delete();

                disposedValue = true;
            }
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
