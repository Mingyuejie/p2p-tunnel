using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
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

        public async Task<MessageRequestResponeWrap> SendReply<T>(SendEventArg<T> arg)
        {
            var res = await serverPluginHelper.SendReply(new MessageRequestParamsWrap<T>
            {
                Connection = arg.Connection,
                Data = arg.Data,
                Path = arg.Path,
                Timeout = arg.Timeout,
            });
            return res;
        }
        public async Task<MessageRequestResponeWrap> SendReply(SendEventArg<byte[]> arg)
        {
            return await serverPluginHelper.SendReply(new MessageRequestParamsWrap<byte[]>
            {
                Connection = arg.Connection,
                Data = arg.Data,
                Path = arg.Path,
                Timeout = arg.Timeout,
            });
        }
        public async Task<bool> SendOnly<T>(SendEventArg<T> arg)
        {
            return await serverPluginHelper.SendOnly(new MessageRequestParamsWrap<T>
            {
                Connection = arg.Connection,
                Data = arg.Data,
                Path = arg.Path,
                Timeout = arg.Timeout,
            });
        }
        public async Task<bool> SendOnly(SendEventArg<byte[]> arg)
        {
            return await serverPluginHelper.SendOnly(new MessageRequestParamsWrap<byte[]>
            {
                Connection = arg.Connection,
                Data = arg.Data,
                Path = arg.Path,
                Timeout = arg.Timeout,
            });
        }
    }
}
