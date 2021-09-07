using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace common.models
{
    public struct Tcpkeepalive
    {
        public uint onoff;
        public uint keepalivetime;
        public uint keepaliveinterval;
    }
}
