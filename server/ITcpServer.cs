using server.model;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public interface ITcpServer : IServer<List<byte>>
    {
        public void BindAccept(int port, IPAddress ip, CancellationTokenSource tokenSource);
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null);

        public bool Send(byte[] data, Socket socket);
    }
}
