using client.plugins.serverPlugins;
using common;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;

namespace client.service.plugins.serverPlugins.heart
{
    public class HeartMessageHelper
    {
        private readonly IServerRequest serverRequest;
        public HeartMessageHelper(IServerRequest serverRequest)
        {
            this.serverRequest = serverRequest;
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
            serverRequest.SendOnly(arg);
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
            serverRequest.SendOnlyTcp(arg);
        }


        public SimplePushSubHandler<OnHeartEventArg> OnHeart { get; } = new SimplePushSubHandler<OnHeartEventArg>();
    }

    public class OnHeartEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public HeartModel Data { get; set; }
    }

}
