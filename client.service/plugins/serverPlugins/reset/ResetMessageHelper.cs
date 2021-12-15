using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using client.service.plugins.serverPlugins.register;
using server.model;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.reset
{
    public class ResetMessageHelper
    {
        private readonly IServerRequest  serverRequest;
        private readonly RegisterState registerState;

        public ResetMessageHelper(IServerRequest serverRequest, RegisterState registerState)
        {
            this.serverRequest = serverRequest;
            this.registerState = registerState;
        }
        private long ConnectId => registerState.RemoteInfo.ConnectId;

        /// <summary>
        /// 发送重启消息
        /// </summary>
        /// <param name="toid"></param>
        public async Task<ServerMessageResponeWrap> SendResetMessage(Socket socket, long toid)
        {
            return await serverRequest.SendReplyTcp(new SendTcpEventArg<ResetModel>
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
