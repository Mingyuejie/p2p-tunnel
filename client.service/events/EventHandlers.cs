using client.service.serverPlugins.register;
using server;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.events
{
    public class EventHandlers
    {
        private readonly ServerPluginHelper  serverPluginHelper;

        public EventHandlers(ServerPluginHelper serverPluginHelper)
        {
            this.serverPluginHelper = serverPluginHelper;
        }

        public long Sequence { get; set; } = 0;


        #region 发送消息
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="arg"></param>
        public async Task<ServerMessageResponeWrap> SendReply<T>(SendEventArg<T> arg)
        {
            return await serverPluginHelper.SendReply(new SendMessageWrap<T>
            {
                Address = arg.Address,
                Data = arg.Data,
                Path = arg.Path,
                Timeout = arg.Timeout,
            });
        }

        public void SendOnly<T>(SendEventArg<T> arg)
        {
            serverPluginHelper.SendOnly(new SendMessageWrap<T>
            {
                Address = arg.Address,
                Data = arg.Data,
                Path = arg.Path,
                Timeout = arg.Timeout,
            });
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        public Task<ServerMessageResponeWrap> SendReplyTcp<T>(SendTcpEventArg<T> arg)
        {
            return serverPluginHelper.SendReplyTcp(new SendMessageWrap<T>
            {
                TcpCoket = arg.Socket,
                Data = arg.Data,
                Timeout = arg.Timeout,
                Path = arg.Path
            });
        }

        public void SendOnlyTcp<T>(SendTcpEventArg<T> arg)
        {
            serverPluginHelper.SendOnlyTcp(new SendMessageWrap<T>
            {
                TcpCoket = arg.Socket,
                Data = arg.Data,
                Timeout = arg.Timeout,
                Path = arg.Path
            });
        }

        #endregion
    }


    #region 发送消息

    public class SendEventArg<T>
    {
        public IPEndPoint Address { get; set; }
        public T Data { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Timeout { get; set; } = 0;
    }
    public class SendTcpEventArg<T>
    {
        public Socket Socket { get; set; }
        public T Data { get; set; }
        public string Path { get; set; } = string.Empty;
        public int Timeout { get; set; } = 0;
    }

    #endregion

}
