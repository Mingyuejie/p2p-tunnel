using client.service.events;
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
        private static readonly Lazy<ResetEventHandles> lazy = new(() => new ResetEventHandles());
        public static ResetEventHandles Instance => lazy.Value;

        private ResetEventHandles()
        {

        }

        private EventHandlers EventHandlers => EventHandlers.Instance;
        private long ConnectId => EventHandlers.ConnectId;

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
            EventHandlers.SendTcp(new SendTcpEventArg
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
