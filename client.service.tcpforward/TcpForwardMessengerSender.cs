using common;
using server;
using server.model;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardMessengerSender
    {
        private readonly MessengerSender messengerSender;
        public TcpForwardMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        #region TCP转发

        public async Task SendTcpForward(SendTcpForwardEventArg arg)
        {
            await messengerSender.SendOnly(new MessageRequestParamsInfo<TcpForwardInfo>
            {
                Path = "TcpForward/Execute",
                Connection = arg.Connection,
                Data = arg.Data
            });
        }

        public SimpleSubPushHandler<OnTcpForwardEventArg> OnTcpForwardHandler { get; } = new SimpleSubPushHandler<OnTcpForwardEventArg>();
        public async Task OnTcpForward(OnTcpForwardEventArg arg)
        {
            await OnTcpForwardHandler.PushAsync(arg);
        }

        #endregion
    }

    public class SendTcpForwardEventArg
    {
        public IConnection Connection { get; set; }
        public TcpForwardInfo Data { get; set; }
    }

    public class OnTcpForwardEventArg
    {
        public IConnection Connection { get; set; }
        public TcpForwardInfo Data { get; set; }
    }
}
