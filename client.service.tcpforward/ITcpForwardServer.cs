using common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public interface ITcpForwardServer
    {
        public void Start(TcpForwardRecordInfo mapping);
        public void Response(TcpForwardInfo model);
        public void Fail(TcpForwardInfo failModel, string body = "");
        public void Stop(int sourcePort);

        public SimpleSubPushHandler<TcpForwardRequestInfo> OnRequest { get; }
        public SimpleSubPushHandler<ListeningChangeInfo> OnListeningChange { get; }
    }
}
