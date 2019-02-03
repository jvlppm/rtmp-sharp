using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net
{
    public partial class SharedObject
    {
        static IDictionary<string, SharedObject> Connected = new Dictionary<string, SharedObject>();
        public static bool TryGetByName(string name, out SharedObject shared)
            => Connected.TryGetValue(name, out shared);

        internal static SharedObject GetRemote(RtmpClient rtmpClient, string name, bool persistent)
        {
            if (!Connected.TryGetValue(name, out SharedObject shared))
            {
                shared = new SharedObject(rtmpClient, name, persistent);
                Connected[name] = shared;
                shared.message.Events.Add(new SharedObjectMessage.ConnectEvent());
                shared.Flush();
            }

            return shared;
        }

        internal static async Task<SharedObject> GetRemoteAsync(RtmpClient rtmpClient, string name, bool persistent)
        {
            SharedObject shared = GetRemote(rtmpClient, name, persistent);
            await shared.initializeCompletion.Task;
            return shared;
        }

        public event EventHandler OnSync;

        public IClientDelegate ClientDelegate;

        public IData Data => data;
        public dynamic DynamicData => data;

        readonly DataAcessor data;
        readonly RtmpClient client;
        readonly object syncMessage = new object();
        readonly TaskCompletionSource<object> initializeCompletion = new TaskCompletionSource<object>();

        SharedObjectMessage message;

        internal SharedObject(RtmpClient client, string name, bool persistent)
        {
            this.data = new DataAcessor(this);
            this.client = client;
            message = new SharedObjectMessage(name, persistent);
        }

        public void Flush()
        {
            lock (syncMessage)
            {
                this.client.InternalSend(message, chunkStreamId: 3);
                message = new SharedObjectMessage(message.Name, message.Persistent) {
                    Version = message.Version + 1
                };
            }
        }

        internal void InternalSync(SharedObjectMessage serverMessage)
        {
            message.Version = serverMessage.Version;
            message.Persistent = serverMessage.Persistent;

            bool? initialization = null;

            foreach (var ev in serverMessage.Events)
            {
                switch (ev)
                {
                    case SharedObjectMessage.ConnectSuccessEvent success:
                        initialization = true;
                        break;

                    case SharedObjectMessage.UpdateDataEvent data:
                        this.data.Properties[data.Name] = data.Value;
                        this.data.FirePropertyChanged(data.Name);
                        break;

					case SharedObjectMessage.DeleteDataEvent data:
						this.data.Properties.Remove(data.Name);
						this.data.FirePropertyChanged(data.Name);
						break;

                    case SharedObjectMessage.SendMessageEvent message:
                        ClientDelegate?.Invoke(message.Name, message.Parameters);
                        break;

                    case SharedObjectMessage.ClearDataEvent clear:
                        this.data.Properties.Clear();
                        break;

                    case SharedObjectMessage.UnsupportedEvent unsupported:
                        Console.WriteLine($"{serverMessage.Name}: {unsupported.Type} (len: {unsupported.Data.Length})");
                        break;
                }
            }

            if (initialization == true) {
                initializeCompletion.TrySetResult(null);
            }

            var handler = OnSync;
            if (handler != null)
            {
                client.CapturedContext.Send(s =>
                {
                    ((EventHandler)s)?.Invoke(this, EventArgs.Empty);
                }, handler);
            }
        }
    }
}
