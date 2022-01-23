using client.servers.clientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardClientPushMsg : IClientPushMsg
    {
        private readonly TcpForwardHelper tcpForwardHelper;
        public TcpForwardClientPushMsg(TcpForwardHelper tcpForwardHelper)
        {
            this.tcpForwardHelper = tcpForwardHelper;
        }

        public IEnumerable<TcpForwardRecordInfoBase> List()
        {
            return tcpForwardHelper.Mappings;
        }
    }
}
