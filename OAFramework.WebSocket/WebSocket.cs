using OAFramework.WebSocket.delegates;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OAFramework.WebSocket
{
    public class WebSocket:IDisposable
    {
        bool AlreadyDisposed;
        Socket Listener;
        int ConnectionQueueLength;
        int MaxBufferSize;
        bool Running;
        Logger logger;
        byte[] FirstByte;
        byte[] LastByte;
        List<Connection> SocketList = new List<Connection>();

        public IPEndPoint LocalEndPoint { get;private set; }
        public event ConnectedEventHandler Connected;
        public event ReceivedEventHandler Received;
        public event DisconnectedEventHandler Disconnected;
        
        private void Initialize()
        {
            AlreadyDisposed = false;
            logger = new Logger();
            ConnectionQueueLength = 500;
            MaxBufferSize = 1024 * 100;
            FirstByte = new byte[MaxBufferSize];
            LastByte = new byte[MaxBufferSize];
            FirstByte[0] = 0x00;
            LastByte[0] = 0xFF;
            logger.enabled = true;
        }
        public WebSocket() => Initialize();
        public WebSocket(IPEndPoint endPoint) => LocalEndPoint = endPoint;
        public void Bind(IPEndPoint endPoint) => LocalEndPoint = endPoint;
        ~WebSocket() => Dispose();
        public void Dispose()
        {
            if (!AlreadyDisposed)
            {
                Running = false;
                AlreadyDisposed = true;
                if (Listener != null) Listener.Close();
                foreach (var item in SocketList)item.Dispose();
                SocketList.Clear();
                GC.SuppressFinalize(this);
            }
        }
        public Task Start()
        {
            Running = true;
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            Listener.Bind(LocalEndPoint);
            Listener.Listen(ConnectionQueueLength);
            return new Task(() =>
            {
                while (Running)
                {
                    Socket NewSock = Listener.Accept();
                    if (NewSock != null)
                    {
                        Thread.Sleep(100);
                        Connection connection = new Connection(NewSock);
                        connection.Connected += new ConnectedEventHandler(Socket_NewConnected);
                        connection.Received += new ReceivedEventHandler(Socket_DataReceived);
                        connection.Disconnected += new DisconnectedEventHandler(Socket_Disconnected);
                        SocketList.Add(connection);
                    }
                }
            });
        }
        public void Send(string message)
        {
            for(var i = 0;i < SocketList.Count; i++)
            {
                var item = SocketList[i];
                if (!item.Socket.Connected) continue;
                item.Send(message);
            }
        }
        public void Send(string username,string message)
        {
            Connection connection = SocketList.Find(what => what.Name == username);
            if (connection != null && connection.Socket.Connected)connection.Send(message);
        }
        public void SendInvalidMsg(string cmd,Connection connection)
        {
            cmd = string.Format("The command invalid \'{0}\'.",cmd);
            try
            {
                if (connection.IsDataMasked)
                {
                    DataFrame dr = new DataFrame(cmd);
                    connection.Socket.Send(dr.GetBytes());
                }
                else
                {
                    connection.Socket.Send(FirstByte);
                    connection.Socket.Send(Encoding.UTF8.GetBytes(cmd));
                    connection.Socket.Send(LastByte);
                }
            }
            catch (Exception e)
            {
                logger.Write(e.ToString());
            }
        }
        private void Handshake(Connection connection)
        {
            if (connection.Socket.Connected)
            {
                try
                {
                    if (connection.IsDataMasked)
                    {
                        DataFrame dr = new DataFrame("ok");
                        connection.Socket.Send(dr.GetBytes());
                    }
                    else
                    {
                        connection.Socket.Send(FirstByte);
                        connection.Socket.Send(Encoding.UTF8.GetBytes("ok"));
                        connection.Socket.Send(LastByte);
                    }
                }
                catch (Exception e)
                {
                    logger.Write(e.ToString());
                }
            }
        }
        private void Socket_Disconnected(Connection connection)
        {
            if(connection != null)
            {
                connection.Socket.Close();
                SocketList.Remove(connection);
                if(Disconnected != null)Disconnected(connection);
            }
        }
        private void Socket_DataReceived(string message, Connection connection)
        {
            string[] args = message.Split(' ');
            if(args != null && args.Length >= 1)
            {
                string command = args[0];
                if(command == "login")
                {
                    if(args.Length > 1)
                    {
                        connection.Name = args[1];
                        Handshake(connection);
                    }
                    else
                    {
                        SendInvalidMsg("login",connection);
                    }
                }
                else if(command == "to")
                {
                    if(args.Length > 2)
                    {
                        args[2] = args[2].TrimStart('\"').TrimEnd('\"');
                        Send(args[1], args[2]);
                    }
                    else
                    {
                        SendInvalidMsg("to", connection);
                    }
                }
                else
                {
                    if(Received != null)Received(message,connection);
                }
            }
        }
        private void Socket_NewConnected(Connection connection)
        {
            if (Connected != null)Connected(connection);
        }
    }
}
