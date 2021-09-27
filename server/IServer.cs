using common;
using server.model;
using System.Net;
using System.Net.Sockets;

namespace server
{
    public interface IServer<T>
    {
        public void Start(int port, IPAddress ip = null);

        public void Stop();

        public SimplePushSubHandler<ServerDataWrap<T>> OnPacket { get; }
    }

    public struct ServerDataWrap<T>
    {
        public T Data;
        public Socket Socket;
        public IPEndPoint Address;
        public ServerType ServerType;
    }
}
