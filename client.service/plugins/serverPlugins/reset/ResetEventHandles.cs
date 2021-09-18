using client.service.plugins.serverPlugins.register;
using server.model;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins.reset
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
