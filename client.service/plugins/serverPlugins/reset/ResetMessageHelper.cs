using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.register;
using server;
using server.model;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.reset
{
    public class ResetMessageHelper
    {
        private readonly IServerRequest  serverRequest;

        public ResetMessageHelper(IServerRequest serverRequest)
        {
            this.serverRequest = serverRequest;
        }

        /// <summary>
        /// 发送重启消息
        /// </summary>
        /// <param name="toid"></param>
        public async Task<MessageRequestResponeWrap> SendResetMessage(IConnection connection, ulong toid)
        {
            return await serverRequest.SendReply(new SendEventArg<ResetModel>
            {
                Connection = connection,
                Path = "reset/Execute",
                Data = new ResetModel
                {
                    ToId = toid
                }
            });
        }
    }
}
