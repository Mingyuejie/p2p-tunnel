using server.model;
using System;
using System.Net.Sockets;

namespace client.service.p2pPlugins.plugins.forward.tcp
{
    public class TcpForwardEventHandles
    {
        private readonly P2PEventHandles p2PEventHandles;
        public TcpForwardEventHandles(P2PEventHandles p2PEventHandles)
        {
            this.p2PEventHandles = p2PEventHandles;
        }

        #region TCP转发
        public event EventHandler<SendTcpForwardEventArg> OnSendTcpForwardHandler;
        public void SendTcpForward(SendTcpForwardEventArg arg)
        {
            p2PEventHandles.SendTcp(new SendP2PTcpArg
            {
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
        public PluginExcuteModel Packet { get; set; }
        public TcpForwardModel Data { get; set; }
    }

    #endregion
}
