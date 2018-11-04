using System;
using System.Net.Sockets;
using System.Threading.Tasks;

// csharp: hina/linq/tcpclientex.cs [snipped]
namespace Hina.Net
{
    static class TcpClientEx
    {
        public static async Task<TcpClient> ConnectAsync(string host, int port, bool exclusiveAddressUse = true)
        {
            /*try {
                return await ConnectTcpDefault(host, port, exclusiveAddressUse);
            }
            catch (Exception) {
            }*/

            return await ConnectTcpCompat(host, port);
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

        static async Task<TcpClient> ConnectTcpCompat(string host, int port)
        {
            var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port);
            return tcp;
        }
    }
}
