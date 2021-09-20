using common;
using server.model;
using server.packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public interface IServer<T>
    {
        public void Start(int port, IPAddress ip = null);

        public void Stop();

        public SimplePushSubHandler<ServerDataWrap<T>> OnPacketPushSub { get; }
    }

    public struct ServerDataWrap<T>
    {
        public T Data;
        public Socket Socket;
        public IPEndPoint Address;
        public ServerType ServerType;
    }
}
