using client.service.events;
using client.service.serverPlugins.register;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;

namespace client.service.serverPlugins.heart
{
    public class HeartEventHandles
    {
        private readonly EventHandlers eventHandlers;
        public HeartEventHandles(EventHandlers eventHandlersr)
        {
            this.eventHandlers = eventHandlersr;
        }


        /// <summary>
        /// 发送心跳消息
        /// </summary>
        public event EventHandler<SendEventArg> OnSendHeartMessageHandler;
        /// <summary>
        /// 发送心跳消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendHeartMessage(long ConnectId, IPEndPoint address)
        {
            SendEventArg arg = new SendEventArg
            {
                Address = address,
                Data = new HeartModel
                {
                    SourceId = ConnectId
                }
            };

            eventHandlers.Send(arg);
            OnSendHeartMessageHandler?.Invoke(this, arg);
        }
        /// <summary>
        /// 发送TCP心跳消息
        /// </summary>
        public event EventHandler<SendTcpEventArg> OnSendTcpHeartMessageHandler;
        /// <summary>
        /// 发送TCP心跳消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpHeartMessage(long ConnectId, Socket socket)
        {
            SendTcpEventArg arg = new SendTcpEventArg
            {
                Socket = socket,
                Data = new HeartModel
                {
                    SourceId = ConnectId
                },
            };
            eventHandlers.SendTcp(arg, 500);
            OnSendTcpHeartMessageHandler?.Invoke(this, arg);
        }

        /// <summary>
        /// 收到心跳信息
        /// </summary>
        public event EventHandler<OnHeartEventArg> OnHeartEventHandler;
        /// <summary>
        /// 收到心跳信息
        /// </summary>
        /// <param name="arg"></param>
        public void OnHeartMessage(OnHeartEventArg arg)
        {
            OnHeartEventHandler?.Invoke(this, arg);
        }
    }

    public class OnHeartEventArg : EventArgs
    {
        public PluginExcuteModel Packet { get; set; }
        public HeartModel Data { get; set; }
    }

}
