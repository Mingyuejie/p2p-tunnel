using client.plugins.serverPlugins;
using common;
using server;
using server.model;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardEventHandles
    {
        private readonly IServerRequest serverRequest;
        public TcpForwardEventHandles(IServerRequest serverRequest)
        {
            this.serverRequest = serverRequest;
        }

        #region TCP转发

        public async Task SendTcpForward(SendTcpForwardEventArg arg)
        {
            await serverRequest.SendOnly(new SendEventArg<TcpForwardModel>
            {
                Path = "TcpForward/Execute",
                Connection = arg.Connection,
                Data = arg.Data
            });
        }

        public SimplePushSubHandler<OnTcpForwardEventArg> OnTcpForwardHandler { get; } = new SimplePushSubHandler<OnTcpForwardEventArg>();
        public async Task OnTcpForward(OnTcpForwardEventArg arg)
        {
            await OnTcpForwardHandler.PushAsync(arg);
        }

        #endregion
    }

    #region TCP转发

    public class SendTcpForwardEventArg : EventArgs
    {
        public IConnection Connection { get; set; }
        public TcpForwardModel Data { get; set; }
    }


    public class OnTcpForwardEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public TcpForwardModel Data { get; set; }
    }

    #endregion
}
