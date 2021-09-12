using client.service.events;
using client.service.serverPlugins.register;
using common;
using server;
using server.model;
using server.models;
using System;
using System.Net;
using System.Net.Sockets;

namespace client.service.serverPlugins.reset
{
    public class ResetEventHandles
    {
        private readonly EventHandlers eventHandlers;
        private readonly RegisterState   registerState;

        public ResetEventHandles(EventHandlers eventHandlers, RegisterState registerState)
        {
            this.eventHandlers = eventHandlers;
            this.registerState = registerState;
        }
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        /// <summary>
        /// 发送重启消息
        /// </summary>
        public event EventHandler<long> OnSendResetMessageHandler;
        /// <summary>
        /// 发送重启消息
        /// </summary>
        /// <param name="toid"></param>
        public void SendResetMessage(Socket socket, long toid)
        {
            eventHandlers.SendTcp(new SendTcpEventArg
            {
                Socket = socket,
                Data = new ResetModel
                {
                    Id = ConnectId,
                    ToId = toid
                }
            });
            OnSendResetMessageHandler?.Invoke(this, toid);
        }

    }
}
