using common.extends;
using server;
using server.model;
using System;
using System.Threading.Tasks;

namespace client.service.messengers.forward
{
    /// <summary>
    /// 来自服务器的消息转发  a->服务器->b
    /// </summary>
    public class ForwardMessenger : IMessenger
    {
        private readonly MessengerResolver  messengerResolver;
        public ForwardMessenger(MessengerResolver messengerResolver)
        {
            this.messengerResolver = messengerResolver;
        }

        public async Task Execute(IConnection connection)
        {
            ForwardParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<ForwardParamsInfo>();

            connection.ReceiveDataWrap.Data = model.Data.AsMemory();
            await messengerResolver.InputData(connection);

        }
    }
}
