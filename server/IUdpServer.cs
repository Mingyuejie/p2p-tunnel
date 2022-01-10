using server.model;
using server.packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public interface IUdpServer : IServer
    {
        public IConnection CreateConnection(IPEndPoint address);
    }
}
