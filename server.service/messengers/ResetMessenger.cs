using common.extends;
using server.model;
using server.service.messengers.register.caching;
using System.Threading.Tasks;

namespace server.service.messengers
{
    public class ResetMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;
        public ResetMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
        }

        public async Task<bool> Execute(IConnection connection)
        {
            ResetParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<ResetParamsInfo>();

            //A已注册
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        return (await messengerSender.SendReply(new MessageRequestParamsInfo<ResetParamsInfo>
                        {
                            Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                            Data = model,
                            Path = connection.ReceiveRequestWrap.Path,
                            RequestId = connection.ReceiveRequestWrap.RequestId
                        })).Code == MessageResponeCodes.OK;
                    }
                }
            }

            return false;
        }
    }
}
