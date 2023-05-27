# OAFramework.WebSocket
A WebSocket framework of the .NET C#


# demo
```
tatic void Main(string[] args)
        {
            var endPoint = new System.Net.IPEndPoint(Util.GetIPV4(), int.Parse(Util.GetConfig("port")));
            var PreMessage = string.Format($"WebSocket Launched ws://{endPoint}/chat");
            WebSocket sock = new WebSocket();
            Console.Title = PreMessage;
            sock.Bind(endPoint);
            sock.Connected += WebSocket_Connected;
            sock.Received += WebSocket_Received;
            sock.Disconnected += WebSocket_Disconnected;
            Task t1 = sock.Start();
            t1.Start();
            Console.WriteLine(PreMessage);
            t1.Wait();
        }

        private static void WebSocket_Disconnected(Connection connection)
        {
            Console.WriteLine($"Disconnect \'{connection.Name}\'.");
        }

        private static void WebSocket_Received(string message, Connection connection)
        {
            Console.WriteLine($"New message \'{message}\' from {connection.Name}.");
            connection.Send($"You Enter: {message}");
        }

        private static void WebSocket_Connected(Connection connection)
        {
            Console.WriteLine($"New connection {connection.Socket.RemoteEndPoint}");
        }
```
