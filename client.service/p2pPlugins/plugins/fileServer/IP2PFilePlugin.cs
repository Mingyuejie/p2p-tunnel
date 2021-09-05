using server.models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.p2pPlugins.plugins.fileServer
{
    public interface IP2PFilePlugin
    {
        P2PFileCmdTypes Type { get; }

        void Excute(TcpFileMessageEventArg arg);
    }
}
