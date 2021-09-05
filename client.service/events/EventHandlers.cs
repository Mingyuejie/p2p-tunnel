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
        public event EventHandler<SendMessageEventArg> OnSendMessageHandler;
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendMessage(SendMessageEventArg arg)
        {
            IPEndPoint address = arg.Address ?? UdpServer;
            if (address == null)
            {
                return;
            }
            UDPServer.Instance.Send(new MessageRecvQueueModel<IMessageModelBase>
            {
                Address = address,
                Data = arg.Data
            });

            OnSendMessageHandler?.Invoke(this, arg);
        }
        /// <summary>
        /// 发送消息
        /// </summary>
        public event EventHandler<SendTcpMessageEventArg> OnSendTcpMessageHandler;

        /// <summary>
        /// 发送消息
        /// </summary>
        public void SendTcpMessage(SendTcpMessageEventArg arg, int timeout = 0)
        {
            if (arg.Socket == null && TcpServer == null)
            {
                return;
            }

            TCPServer.Instance.Send(new MessageRecvQueueModel<IMessageModelBase>
            {
                TcpCoket = arg.Socket ?? TcpServer,
                Data = arg.Data,
                Timeout = timeout
            });

            OnSendTcpMessageHandler?.Invoke(this, arg);
        }

        #endregion
    }


    #region 发送消息

    public class SendMessageEventArg
    {
        /// <summary>
        /// 为 null时默认给连接的服务器发送
        /// </summary>
        public IPEndPoint Address { get; set; }
        public IMessageModelBase Data { get; set; }
    }
    public class SendTcpMessageEventArg
    {
        /// <summary>
        /// 为 null时默认给连接的服务器发送
        /// </summary>
        public Socket Socket { get; set; }
        public IMessageModelBase Data { get; set; }
    }

    #endregion

}
