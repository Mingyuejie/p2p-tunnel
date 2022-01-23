using server;
using server.model;
using System.Threading.Tasks;

namespace cclient.service.messengers.reset
{
    public class ResetMessengerSender
    {
        private readonly MessengerSender messengerSender;

        public ResetMessengerSender(MessengerSender messengerSender)
        {
            this.messengerSender = messengerSender;
        }

        /// <summary>
        /// 发送重启消息
        /// </summary>
        /// <param name="toid"></param>
        public async Task<MessageResponeInfo> SendResetMessage(IConnection connection, ulong toid)
        {
            return await messengerSender.SendReply(new MessageRequestParamsInfo<ResetParamsInfo>
            {
                Connection = connection,
                Path = "reset/Execute",
                Data = new ResetParamsInfo
                {
                    ToId = toid
                }
            });
        }
    }
}
