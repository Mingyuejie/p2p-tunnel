using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public interface IUdpServer
    {
        public void Start(int port, IPAddress ip = null);
        public void Stop();
        public Task<ServerResponeMessageWrap> SendReply<T>(RecvQueueModel<T> msg);
        public void SendOnly<T>(RecvQueueModel<T> msg);
    }
}
