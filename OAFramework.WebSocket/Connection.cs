using OAFramework.WebSocket.delegates;
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace OAFramework.WebSocket
{
    public class Connection:IDisposable
    {
        private Logger logger;
        public string Name { get; set; }
        public string Identifier { get; private set; }
        public bool IsDataMasked;
        public Socket Socket { get; private set; }
        public byte[] receivedDataBuffer;

        private int MaxBufferSize;
        private string Handshake;
        private string New_Handshake;
        private byte[] FirstByte,LastByte,ServerKey1, ServerKey2;

        public event ConnectedEventHandler    Connected;
        public event ReceivedEventHandler     Received;
        public event DisconnectedEventHandler Disconnected;

        public Connection(Socket _Socket)
        {
            Name = string.Empty;
            Identifier = Guid.NewGuid().ToString("N");
            Identifier = Identifier.Replace("-", "");
            Socket = _Socket;
            logger = new Logger();
            MaxBufferSize = 1024 * 100;
            receivedDataBuffer = new byte[MaxBufferSize];
            FirstByte = new byte[MaxBufferSize];
            LastByte = new byte[MaxBufferSize];
            FirstByte[0] = 0x00;
            LastByte[0] = 0xFF;

            Handshake = "HTTP/1.1 101 Web Socket Protocol Handshake" + Environment.NewLine;
            Handshake += "Upgrade: WebSocket" + Environment.NewLine; 
            Handshake += "Connection: Upgrade" + Environment.NewLine;
            Handshake += "Sec-WebSocket-Origin: " + "{0}" + Environment.NewLine;
            Handshake += string.Format("Sec-WebSocket-Location: " + "ws://{0}/chat" + Environment.NewLine,Socket.LocalEndPoint.ToString());
            Handshake += Environment.NewLine;

            New_Handshake = "HTTP/1.1 101 Switching Protocols" + Environment.NewLine;
            New_Handshake += "Upgrade: WebSocket" + Environment.NewLine;
            New_Handshake += "Connection: Upgrade" + Environment.NewLine;
            New_Handshake += "Sec-WebSocket-Accept: {0}" + Environment.NewLine;
            New_Handshake += Environment.NewLine;

            Socket.BeginReceive(receivedDataBuffer, 0, receivedDataBuffer.Length,
                SocketFlags.None, new AsyncCallback(ManageHanshake), Socket.Available);
        }
        private void Read(IAsyncResult status)
        {
            if (!Socket.Connected)return;
            string messageReceived = string.Empty;
            DataFrame dr = new DataFrame(receivedDataBuffer);
            try
            {
                if (!IsDataMasked)
                {
                    // Web Socket protocol: messages are sent with 0x00 and 0xff as padding bytes
                    UTF8Encoding decoder = new UTF8Encoding();
                    int startIndex = 0;
                    int endIndex = 0;
                    // Search for the start type
                    while (receivedDataBuffer[endIndex] == FirstByte[0]) startIndex++;
                    // Search for the end byte
                    while (receivedDataBuffer[endIndex] != LastByte[0] && endIndex != MaxBufferSize - 1) endIndex++;
                    if (endIndex == MaxBufferSize - 1) endIndex = MaxBufferSize;
                    // Get the message
                    messageReceived = decoder.GetString(receivedDataBuffer, startIndex, endIndex - startIndex);
                }
                else
                {
                    messageReceived = dr.Text;
                }
                if((messageReceived.Length == MaxBufferSize && messageReceived[0] == Convert.ToChar(65533)) || messageReceived.Length == 0)
                {
                    if (Disconnected != null)Disconnected(this);
                }
                else
                {
                    if(Received != null)
                    {
                        Received(messageReceived,this);
                    }
                    Array.Clear(receivedDataBuffer, 0, receivedDataBuffer.Length);
                    Socket.BeginReceive(receivedDataBuffer, 0, receivedDataBuffer.Length, 0, new AsyncCallback(Read), null);
                }
            }catch(Exception ex)
            {
                logger.Write(ex.ToString());
                if (Disconnected != null)Disconnected(this);
            }
        }
        public void Send(string message)
        {
            try
            {
                if (IsDataMasked)
                {
                    DataFrame dr = new DataFrame(message);
                    Socket.Send(dr.GetBytes());
                }
                else
                {
                    Socket.Send(FirstByte);
                    Socket.Send(Encoding.UTF8.GetBytes(message));
                    Socket.Send(LastByte);
                }
            }catch(Exception e)
            {
                if (Disconnected != null)Disconnected(this);
            }
        }
        private void BuildServerPartialKey(int keyNum,string clientKey)
        {
            string partialServerKey = string.Empty;
            byte[] currentKey;
            int spacesNum = 0;
            char[] keyChars = clientKey.ToCharArray();
            foreach(var currentChar in keyChars)
            {
                if (char.IsDigit(currentChar)) partialServerKey += currentChar;
                if (char.IsWhiteSpace(currentChar)) spacesNum++;
            }
            try
            {
                currentKey = BitConverter.GetBytes((int)(Int64.Parse(partialServerKey) / spacesNum));
                if (BitConverter.IsLittleEndian) Array.Reverse(currentKey);
                if (keyNum == 1) ServerKey1 = currentKey;
                else ServerKey2 = currentKey;
            }
            catch
            {
                if (ServerKey1 != null) Array.Clear(ServerKey1, 0, ServerKey1.Length);
                if (ServerKey2 != null) Array.Clear(ServerKey2, 0, ServerKey2.Length);
            }
        }
        private byte[] BuildServerFullKey(byte[] last8Bytes)
        {
            byte[] concatenatedKeys = new byte[16];
            Array.Copy(ServerKey1, 0, concatenatedKeys, 0, 4);
            Array.Copy(ServerKey2, 0, concatenatedKeys, 4, 4);
            Array.Copy(last8Bytes, 0, concatenatedKeys, 8, 8);
            // MD5 Hash
            MD5 md5 = MD5.Create();
            return md5.ComputeHash(concatenatedKeys);
        }
        private void ManageHanshake(IAsyncResult status)
        {
            string header = "Sec-WebSocket-Version:";
            int HandshakeLength = (int)status.AsyncState;
            byte[] last8Bytes = new byte[8];
            UTF8Encoding decoder = new UTF8Encoding();
            String rawClientHandshake = decoder.GetString(receivedDataBuffer, 0, HandshakeLength);
            Array.Copy(receivedDataBuffer, HandshakeLength - 8, last8Bytes, 0, 8);
            // Now using the newer WebSocket protocol.
            if (rawClientHandshake.IndexOf(header) != -1)
            {
                IsDataMasked = true;
                string[] rawClientHandshakeLines = rawClientHandshake.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                string acceptKey = string.Empty;
                foreach (var line in rawClientHandshakeLines)
                {
                    //Console.WriteLine(line);
                    if (line.Contains("Sec-WebSocket-Key:")) acceptKey = ComputeWebSocketHandshakeSecurityHash09(line.Substring(line.IndexOf(":") + 2));
                }
                New_Handshake = string.Format(New_Handshake, acceptKey);
                byte[] newHandshakeText = Encoding.UTF8.GetBytes(New_Handshake);
                Socket.BeginSend(newHandshakeText, 0, newHandshakeText.Length, 0, HandshakeFinished, null);
                return;
            }
            string ClientHandshake = decoder.GetString(receivedDataBuffer, 0, HandshakeLength - 8);
            string[] ClientHandshakeLines = ClientHandshake.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            // Welcome the new client
            foreach (var line in ClientHandshakeLines)
            {
                //logger.d(line);
                if (line.Contains("Sec-WebSocket-Key1:")) BuildServerPartialKey(1, line.Substring(line.IndexOf(":") + 2));
                if (line.Contains("Sec-WebSocket-Key2:")) BuildServerPartialKey(2, line.Substring(line.IndexOf(":") + 2));
                if (line.Contains("Origin:"))
                {
                    try
                    {
                        Handshake = string.Format(Handshake, line.Substring(line.IndexOf(":") + 2));
                    }
                    catch
                    {
                        Handshake = string.Format(Handshake, "null");
                    }
                }
            }
            // Build the response for the client
            byte[] HandshakeText = Encoding.UTF8.GetBytes(Handshake);
            byte[] serverHandshakeResponse = new byte[HandshakeText.Length + 16];
            byte[] serverKey = BuildServerFullKey(last8Bytes);
            Array.Copy(HandshakeText, serverHandshakeResponse, HandshakeText.Length);
            Array.Copy(serverKey, 0, serverHandshakeResponse, HandshakeText.Length, 16);
            //logger.d("Send handshake message...");
            Socket.BeginSend(serverHandshakeResponse, 0, HandshakeText.Length + 16, 0, HandshakeFinished, null);
            //logger.d(Handshake);
        }
        private static String ComputeWebSocketHandshakeSecurityHash09(String secWebSocketKey)
        {
            const String MagicKEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
            String secWebSocketAccept = String.Empty;
            // 1. Combine the request Sec-WebSocket-Key with magic key.
            String ret = secWebSocketKey + MagicKEY;
            // 2. Compute the SHA1 hash
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] sha1Hash = sha.ComputeHash(Encoding.UTF8.GetBytes(ret));
            // 3. Base64 encode the hash
            secWebSocketAccept = Convert.ToBase64String(sha1Hash);
            return secWebSocketAccept;
        }
        private void HandshakeFinished(IAsyncResult status)
        {
            Socket.EndSend(status);
            Socket.BeginReceive(receivedDataBuffer, 0, receivedDataBuffer.Length, 0, new AsyncCallback(Read), null);
            if (Connected != null)Connected(this);
        }
        public void Dispose()
        {
            if(Socket != null)
            {
                Socket.Close();
                Socket = null;
            }
        }
    }
}