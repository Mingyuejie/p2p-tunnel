using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.service
{
    public class Config
    {
        public int Udp { get; set; } = 0;
        public int Tcp { get; set; } = 0;
        public int TcpBufferSize { get; set; } = 8 * 1024;

        public bool tcpForward { get; set; } = false;
    }
}
