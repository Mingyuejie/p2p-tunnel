using client.plugins.serverPlugins;
using common;
using server.model;
using System;
using System.Net.Sockets;

namespace client.service.tcpforward
{
    public class TcpForwardEventHandles
    {
        private readonly IServerRequest  serverRequest;
        public TcpForwardEventHandles(IServerRequest serverRequest)
        {
            this.serverRequest = serverRequest;
        }

        #region TCP转发
        
        public void SendTcpForward(SendTcpForwardEventArg arg)
        {
            serverRequest.SendOnlyTcp(new SendTcpEventArg<TcpForwardModel>
            {
                Path = "TcpForward/excute",
                Socket = arg.Socket,
                Data = arg.Data
            });
        }

        public SimplePushSubHandler<OnTcpForwardEventArg> OnTcpForwardHandler { get; } = new SimplePushSubHandler<OnTcpForwardEventArg>();
        public void OnTcpForward(OnTcpForwardEventArg arg)
        {
            OnTcpForwardHandler.Push(arg);
        }

        #endregion
    }

    #region TCP转发

    public class SendTcpForwardEventArg : EventArgs
    {
        public Socket Socket { get; set; }
        public TcpForwardModel Data { get; set; }
    }


    public class OnTcpForwardEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public TcpForwardModel Data { get; set; }
    }

    #endregion
}
