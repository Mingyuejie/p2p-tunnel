using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System.Net;

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

        public bool Execute(PluginParamWrap data)
        {
            PunchHoleModel model = data.Wrap.Memory.DeBytes<PunchHoleModel>();

            //A已注册
            if (!clientRegisterCache.Get(data.Connection.ConnectId, out RegisterCacheModel source))
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
                model.Data = new PunchHoleNotifyModel
                {
                    Ip = source.UdpConnection.UdpAddress.Address.ToString(),
                    Name = source.Name,
                    Port = source.UdpConnection.UdpAddress.Port,
                    TcpPort = (source.TcpConnection.TcpSocket.RemoteEndPoint as IPEndPoint).Port,
                    LocalIps = source.LocalIps,
                    LocalTcpPort = source.LocalTcpPort,
                    LocalUdpPort = source.LocalUdpPort,
                }.ToBytes();
            }

            model.FromId = data.Connection.ConnectId;
            serverPluginHelper.SendOnly(new MessageRequestParamsWrap<PunchHoleModel>
            {
                Connection = data.Connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                Data = model,
                Path = data.Wrap.Path,
                RequestId = data.Wrap.RequestId
            });

            return true;
        }
    }
}
