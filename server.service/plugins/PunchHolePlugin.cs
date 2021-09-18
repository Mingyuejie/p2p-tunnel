using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.cache;
using server.service.model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.service.plugins
{
    public class PunchHolePlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        private readonly ServerPluginHelper serverPluginHelper;

        public PunchHolePlugin(IClientRegisterCache clientRegisterCache, ServerPluginHelper serverPluginHelper)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.serverPluginHelper = serverPluginHelper;
        }

        public bool Excute(PluginParamWrap data)
        {
            PunchHoleModel model = data.Wrap.Content.DeBytes<PunchHoleModel>();

            if (!clientRegisterCache.Verify(model.Id, data)) return false;

            //A已注册
            RegisterCacheModel source = clientRegisterCache.Get(model.Id);
            if (source == null) return false;
            //B已注册
            RegisterCacheModel target = clientRegisterCache.Get(model.ToId);
            if (target == null) return false;

            //是否在同一个组
            if (source.GroupId != target.GroupId) return false;

            if (model.PunchForwardType == PunchForwardTypes.NOTIFY)
            {
                model.Data = new PunchHoleNotifyModel
                {
                    Ip = source.Address.Address.ToString(),
                    Id = source.Id,
                    Name = source.Name,
                    Port = source.Address.Port,
                    TcpPort = source.TcpPort,
                    LocalIps = source.LocalIps,
                    LocalTcpPort = source.LocalTcpPort,
                    LocalUdpPort = source.LocalUdpPort,
                }.ToBytes();
            }
            switch (data.ServerType)
            {
                case ServerType.TCP:
                    {
                        serverPluginHelper.SendOnlyTcp(new SendMessageWrap<PunchHoleModel>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model,
                            Code = ServerMessageResponeCodes.OK,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId
                        });
                    }
                    break;
                case ServerType.UDP:
                    {
                        serverPluginHelper.SendOnly(new SendMessageWrap<PunchHoleModel>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model,
                            Code = ServerMessageResponeCodes.OK,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId
                        });
                    }
                    break;
                default:
                    break;
            }

            return true;
        }
    }
}
