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
    public interface ITcpServer : IServer<TcpPacket[]>
    {
        public void BindAccept(int port, IPAddress ip, CancellationTokenSource tokenSource);
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null,long connectId = 0);

        public bool Send(byte[] data, Socket socket);
    }
}
