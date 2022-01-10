using server.model;
using server.packet;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public interface ITcpServer : IServer
    {
        public void BindAccept(int port, IPAddress ip);
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null);

        public IConnection CreateConnection(Socket socket);
    }
}
