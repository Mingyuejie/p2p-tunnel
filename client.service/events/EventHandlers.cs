using server;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;

namespace client.service.events
{
    public class EventHandlers
    {
        private static readonly Lazy<EventHandlers> lazy = new Lazy<EventHandlers>(() => new EventHandlers());
        public static EventHandlers Instance => lazy.Value;

        private EventHandlers()
        {

        }

        public long Sequence { get; set; } = 0;
        public IPEndPoint UdpServer => AppShareData.Instance.UdpServer;
        public Socket TcpServer  => AppShareData.Instance.TcpServer;
        public long ConnectId => AppShareData.Instance.RemoteInfo.ConnectId;
        public int ClientTcpPort => AppShareData.Instance.LocalInfo.TcpPort;
        public int RouteLevel => AppShareData.Instance.LocalInfo.RouteLevel;


        #region 发送消息
        /// <summary>
        /// 发送消息
        /// </summary>
        public event EventHandler<SendEventArg> OnSendHandler;
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="arg"></param>
        public void Send(SendEventArg arg)
        {
            IPEndPoint address = arg.Address ?? UdpServer;
            if (address == null)
            {
                return;
            }
            UDPServer.Instance.Send(new RecvQueueModel<IModelBase>
            {
                Address = address,
                Data = arg.Data
            });

            OnSendHandler?.Invoke(this, arg);
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        public event EventHandler<SendTcpEventArg> OnSendTcpHandler;

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendTcp(SendTcpEventArg arg, int timeout = 0)
        {
            if (arg.Socket == null && TcpServer == null)
            {
                return;
            }

            TCPServer.Instance.Send(new RecvQueueModel<IModelBase>
            {
                TcpCoket = arg.Socket ?? TcpServer,
                Data = arg.Data,
                Timeout = timeout
            });

            OnSendTcpHandler?.Invoke(this, arg);
        }

        #endregion
    }


    #region 发送消息

    public class SendEventArg
    {
        /// <summary>
        /// 为 null时默认给连接的服务器发送
        /// </summary>
        public IPEndPoint Address { get; set; }
        public IModelBase Data { get; set; }
    }
    public class SendTcpEventArg
    {
        /// <summary>
        /// 为 null时默认给连接的服务器发送
        /// </summary>
        public Socket Socket { get; set; }
        public IModelBase Data { get; set; }
    }

    #endregion

}
