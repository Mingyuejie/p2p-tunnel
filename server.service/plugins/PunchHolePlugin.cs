using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System.Net;
using System.Threading.Tasks;

namespace server.service.plugins
{
    public class PunchHolePlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly ServerPluginHelper serverPluginHelper;

        public PunchHolePlugin(IClientRegisterCaching clientRegisterCache, ServerPluginHelper serverPluginHelper)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.serverPluginHelper = serverPluginHelper;
        }

        public async Task<bool> Execute(IConnection connection)
        {
            PunchHoleModel model = connection.ReceiveRequestWrap.Memory.DeBytes<PunchHoleModel>();

            //A已注册
            if (!clientRegisterCache.Get(connection.ConnectId, out RegisterCacheModel source))
            {
                return false;
            }
            //B已注册
            if (!clientRegisterCache.Get(model.ToId, out RegisterCacheModel target))
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
                if (!source.GetTunnel(model.TunnelName, out TunnelRegisterCacheModel tunnel))
                {
                    return false;
                }

                model.Data = new PunchHoleNotifyModel
                {
                    Ip = source.UdpConnection.UdpAddress.Address.ToString(),
                    LocalIps = source.LocalIps,
                    LocalPort = tunnel.LocalPort,
                    Port = tunnel.Port,
                    IsDefault = tunnel.IsDefault
                }.ToBytes();
            }

            model.FromId = connection.ConnectId;
            return await serverPluginHelper.SendOnly(new MessageRequestParamsWrap<PunchHoleModel>
            {
                Connection = connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                Data = model,
                Path = connection.ReceiveRequestWrap.Path,
                RequestId = connection.ReceiveRequestWrap.RequestId
            });
        }
    }
}
