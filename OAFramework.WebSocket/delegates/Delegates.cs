namespace OAFramework.WebSocket.delegates
{
    public delegate void ConnectedEventHandler(Connection connection);
    public delegate void ReceivedEventHandler(string message, Connection connection);
    public delegate void DisconnectedEventHandler(Connection connection);
}