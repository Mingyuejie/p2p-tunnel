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
        public void Send(RecvQueueModel<IModelBase> msg);
    }
}
