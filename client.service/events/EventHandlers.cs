using client.service.serverPlugins.register;
using server;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;

namespace client.service.events
{
    public class EventHandlers
    {
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;

        public EventHandlers(ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
        }

        public long Sequence { get; set; } = 0;


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
            if (arg.Address == null)
            {
                return;
            }
            Console.WriteLine($"-----{arg.Address}");
            udpServer.Send(new RecvQueueModel<IModelBase>
            {
                Address = arg.Address,
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
            if (arg.Socket == null)
            {
                return;
            }

            tcpServer.Send(new RecvQueueModel<IModelBase>
            {
                TcpCoket = arg.Socket,
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
