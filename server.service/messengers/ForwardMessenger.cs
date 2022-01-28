using common.extends;
using server.model;
using server.service.messengers.register.caching;
using System.Threading.Tasks;

namespace server.service.messengers
{
    /// <summary>
    /// P2P数据转发
    /// </summary>
    public class ForwardMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerResolver messengerResolver;
        private readonly MessengerSender messengerSender;
        public ForwardMessenger(IClientRegisterCaching clientRegisterCache, MessengerResolver messengerResolver, MessengerSender messengerSender)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerResolver = messengerResolver;
            this.messengerSender = messengerSender;
        }

        public async Task Execute(IConnection connection)
        {
            ForwardParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<ForwardParamsInfo>();

            //A已注册
            if (clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        await messengerSender.SendOnly(new MessageRequestParamsInfo<byte[]>
                        {
                            Connection = connection,
                            Data = model.Data,
                            MemoryPath = connection.ReceiveRequestWrap.Path,
                            RequestId = connection.ReceiveRequestWrap.RequestId
                        });
                    }
                }
            }
        }
    }
}
