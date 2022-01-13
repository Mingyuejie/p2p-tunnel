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
        public void Start(TcpForwardRecordModel mapping);
        public void Response(TcpForwardModel model);
        public void Fail(TcpForwardModel failModel, string body = "");
        public void Stop(int sourcePort);

        public SimpleSubPushHandler<TcpForwardRequestModel> OnRequest { get; }
        public SimpleSubPushHandler<ListeningChangeModel> OnListeningChange { get; }
    }
}
