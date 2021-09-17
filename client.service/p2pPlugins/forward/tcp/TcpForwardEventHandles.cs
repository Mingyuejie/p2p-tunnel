using client.service.events;
using server.model;
using System;
using System.Net.Sockets;

namespace client.service.p2pPlugins.forward.tcp
{
    public class TcpForwardEventHandles
    {
        private readonly EventHandlers eventHandlers;
        public TcpForwardEventHandles(EventHandlers eventHandlers)
        {
            this.eventHandlers = eventHandlers;
        }

        #region TCP转发
        public event EventHandler<SendTcpForwardEventArg> OnSendTcpForwardHandler;
        public void SendTcpForward(SendTcpForwardEventArg arg)
        {
            eventHandlers.SendOnlyTcp(new SendTcpEventArg<TcpForwardModel>
            {
                Path = "TcpForward/excute",
                Socket = arg.Socket,
                Data = arg.Data
            });

            OnSendTcpForwardHandler?.Invoke(this, arg);
        }

        public event EventHandler<OnTcpForwardEventArg> OnTcpForwardHandler;
        public void OnTcpForward(OnTcpForwardEventArg arg)
        {
            OnTcpForwardHandler?.Invoke(this, arg);
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
