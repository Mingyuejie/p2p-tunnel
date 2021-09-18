using server.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public interface IUdpServer : IServer<byte[]>
    {
        public bool Send(byte[] data, IPEndPoint address);
    }
}
