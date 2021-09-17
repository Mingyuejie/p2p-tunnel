using client.service.events;
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
        /// <param name="arg"></param>
        public void SendHeartMessage(long ConnectId, IPEndPoint address)
        {
            SendEventArg<HeartModel> arg = new SendEventArg<HeartModel>
            {
                Address = address,
                Path = "heart/excute",
                Data = new HeartModel
                {
                    SourceId = ConnectId
                }
            };

            eventHandlers.SendOnly(arg);
        }
        /// <summary>
        /// 发送TCP心跳消息
        /// </summary>
        /// <param name="arg"></param>
        public void SendTcpHeartMessage(long ConnectId, Socket socket)
        {
            SendTcpEventArg<HeartModel> arg = new SendTcpEventArg<HeartModel>
            {
                Socket = socket,
                Path = "heart/excute",
                Data = new HeartModel
                {
                    SourceId = ConnectId
                },
                Timeout = 500
            };
            eventHandlers.SendOnlyTcp(arg);
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
        public PluginParamWrap Packet { get; set; }
        public HeartModel Data { get; set; }
    }

}
