using server.model;
using System;
using System.Net.Sockets;

namespace client.service.p2pPlugins.plugins.forward.tcp
{
    public class TcpForwardEventHandles
    {
        private static readonly Lazy<TcpForwardEventHandles> lazy = new Lazy<TcpForwardEventHandles>(() => new TcpForwardEventHandles());
        public static TcpForwardEventHandles Instance => lazy.Value;

        private TcpForwardEventHandles()
        {

        }

        #region TCP转发
        public event EventHandler<SendTcpForwardMessageEventArg> OnSendTcpForwardMessageHandler;
        public void SendTcpForwardMessage(SendTcpForwardMessageEventArg arg)
        {
            P2PMessageEventHandles.Instance.SendTcpMessage(new SendP2PTcpMessageArg
            {
                Socket = arg.Socket,
                Data = arg.Data
            });

            OnSendTcpForwardMessageHandler?.Invoke(this, arg);
        }

        public event EventHandler<OnTcpForwardMessageEventArg> OnTcpForwardMessageHandler;
        public void OnTcpForwardMessage(OnTcpForwardMessageEventArg arg)
        {
            OnTcpForwardMessageHandler?.Invoke(this, arg);
        }
        #endregion
    }

    #region TCP转发

    public class SendTcpForwardMessageEventArg : EventArgs
    {
        public Socket Socket { get; set; }
        public TcpForwardModel Data { get; set; }
    }


    public class OnTcpForwardMessageEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public TcpForwardModel Data { get; set; }
    }

    #endregion
}
