using OAFramework.WebSocket.utils;
using System;

namespace OAFramework.WebSocket.TestUnit
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.Title = "OAFramework.WebSocket";
            WebSocket sock = new WebSocket();
            var ipep = new System.Net.IPEndPoint(Util.GetIPV4(), 1106);
            sock.Bind(ipep);
            sock.Connected += WebSocket_Connected;
            sock.Received += WebSocket_Received;
            sock.Disconnected += WebSocket_Disconnected;
            sock.Start();
        }

        private static void WebSocket_Disconnected(Connection connection)
        {
            Console.WriteLine("已下线\'{0}\'.", connection.Name);
        }

        private static void WebSocket_Received(string message, Connection connection)
        {
            Console.WriteLine("新消息:{1}.", connection.Name, message);
            connection.Send("You Enter: " + message);
        }

        private static void WebSocket_Connected(Connection connection)
        {
            Console.WriteLine("新的连接:{0}", connection.Socket.RemoteEndPoint.ToString());
            connection.Send("Hello,Boy!");
        }
    }
}
