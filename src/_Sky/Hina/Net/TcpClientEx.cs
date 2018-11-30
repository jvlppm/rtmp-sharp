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
            TcpClient tcp;
            try
            {
                tcp = new TcpClient(AddressFamily.InterNetworkV6);
                Console.WriteLine($"Trying to connect to host ipv6: {host}");
                await tcp.ConnectAsync(host, port);
            }
            catch
            {
                tcp = new TcpClient();
                Console.WriteLine($"Trying to connect to host: {host}"); 
                await tcp.ConnectAsync(host, port);
            }
            return tcp;
        }
    }
}
