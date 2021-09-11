using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public interface ITcpServer
    {
        public void Start(int port, IPAddress ip = null);
        public void BindAccept(int port, IPAddress ip = null);
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null);
        public void Stop();
        public void Send(RecvQueueModel<IModelBase> msg);
    }
}
