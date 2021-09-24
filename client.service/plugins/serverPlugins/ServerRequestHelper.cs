﻿using client.plugins.serverPlugins;
using server;
using server.model;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins
{
    public class ServerRequestHelper : IServerRequest
    {
        private readonly ServerPluginHelper serverPluginHelper;

        public ServerRequestHelper(ServerPluginHelper serverPluginHelper)
        {
            this.serverPluginHelper = serverPluginHelper;
        }

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
    }
}