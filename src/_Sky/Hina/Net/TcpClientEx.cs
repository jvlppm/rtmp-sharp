using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using RtmpSharp._Sky.Hina.Extensions;

// csharp: hina/linq/tcpclientex.cs [snipped]
namespace Hina.Net
{
    static class TcpClientEx
    {
        public static async Task<TcpClient> ConnectAsync(string host, int port, bool exclusiveAddressUse = true, AddressFamily? connectionMode = null)
        {
            /*try {
                return await ConnectTcpDefault(host, port, exclusiveAddressUse);
            }
            catch (Exception) {
            }*/

            return await ConnectTcpCompat(host, port, connectionMode);
        }

        static async Task<TcpClient> ConnectTcpDefault(string host, int port, bool exclusiveAddressUse)
        {
            var tcp = new TcpClient
            {
                NoDelay = true,
                ExclusiveAddressUse = exclusiveAddressUse
            };

            SocketEx.FastSocket(tcp.Client);

            await tcp.ConnectAsync(host, port);
            return tcp;
        }

        static async Task<TcpClient> ConnectTcpCompat(string host, int port, AddressFamily? family)
        {
            var tcp = family == null? new TcpClient() : new TcpClient(family.Value);
            await tcp.ConnectAsync(host, port);
            return tcp;
        }
    }
}
