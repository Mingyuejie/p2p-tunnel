using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardMessenger : IMessenger
    {
        private readonly TcpForwardMessengerSender  tcpForwardMessengerClient;
        public TcpForwardMessenger(TcpForwardMessengerSender tcpForwardMessengerClient)
        {
            this.tcpForwardMessengerClient = tcpForwardMessengerClient;
        }

        public async Task Execute(IConnection connection)
        {
            var data = connection.ReceiveRequestWrap.Memory.DeBytes<TcpForwardInfo>();
            await tcpForwardMessengerClient.OnTcpForward(new OnTcpForwardEventArg
            {
                Connection = connection,
                Data = data,
            });
        }
    }

}
