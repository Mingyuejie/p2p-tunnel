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
        public event EventHandler<SendTcpForwardEventArg> OnSendTcpForwardHandler;
        public void SendTcpForward(SendTcpForwardEventArg arg)
        {
            P2PEventHandles.Instance.SendTcp(new SendP2PTcpArg
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
