using client.service.events;
using client.service.serverPlugins.register;
using common;
using server;
using server.model;
using server.models;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.serverPlugins.reset
{
    public class ResetEventHandles
    {
        private readonly EventHandlers eventHandlers;
        private readonly RegisterState registerState;

        public ResetEventHandles(EventHandlers eventHandlers, RegisterState registerState)
        {
            this.eventHandlers = eventHandlers;
            this.registerState = registerState;
        }
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        /// <summary>
        /// 发送重启消息
        /// </summary>
        /// <param name="toid"></param>
        public async Task<ServerMessageResponeWrap> SendResetMessage(Socket socket, long toid)
        {
            return await eventHandlers.SendReplyTcp(new SendTcpEventArg<ResetModel>
            {
                Socket = socket,
                Path = "reset/excute",
                Data = new ResetModel
                {
                    Id = ConnectId,
                    ToId = toid
                }
            });
        }
    }
}
