using common.extends;
using server.model;
using server.service.messengers.register.caching;
using System.Text;
using System.Threading.Tasks;

namespace server.service.messengers
{
    public class PunchHoleMessenger : IMessenger
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly MessengerSender messengerSender;

        public PunchHoleMessenger(IClientRegisterCaching clientRegisterCache, MessengerSender messengerSender)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.messengerSender = messengerSender;
        }

        public async Task<bool> Execute(IConnection connection)
        {
            PunchHoleParamsInfo model = connection.ReceiveRequestWrap.Memory.DeBytes<PunchHoleParamsInfo>();

            //A已注册
            if (!clientRegisterCache.Get(connection.ConnectId, out RegisterCacheInfo source))
            {
                return false;
            }
            //B已注册
            if (!clientRegisterCache.Get(model.ToId, out RegisterCacheInfo target))
            {
                return false;
            }

            //是否在同一个组
            if (source.GroupId != target.GroupId)
            {
                return false;
            }

            if (model.PunchForwardType == PunchForwardTypes.NOTIFY)
            {
                if (!source.GetTunnel(model.TunnelName, out TunnelRegisterCacheInfo tunnel))
                {
                    return false;
                }

                model.Data = new PunchHoleNotifyInfo
                {
                    Ip = source.UdpConnection.Address.Address.ToString(),
                    LocalIps = source.LocalIps,
                    LocalPort = tunnel.LocalPort,
                    Port = tunnel.Port,
                    IsDefault = tunnel.IsDefault
                }.ToBytes();
            }

            model.FromId = connection.ConnectId;
            return await messengerSender.SendOnly(new MessageRequestParamsInfo<PunchHoleParamsInfo>
            {
                Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                Data = model,
                MemoryPath = connection.ReceiveRequestWrap.Path,
                RequestId = connection.ReceiveRequestWrap.RequestId
            });
        }
    }
}
