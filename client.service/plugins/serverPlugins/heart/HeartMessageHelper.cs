using client.plugins.serverPlugins;
using common;
using server;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

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
        public async Task<MessageRequestResponeWrap> SendHeartMessage(IConnection connection)
        {
            SendEventArg<HeartModel> arg = new SendEventArg<HeartModel>
            {
                Connection = connection,
                Path = "heart/Execute",
                Data = new HeartModel{}
            };
            return await serverRequest.SendReply(arg);
        }
    }

    public class OnHeartEventArg : EventArgs
    {
        public PluginParamWrap Packet { get; set; }
        public HeartModel Data { get; set; }
    }

}
