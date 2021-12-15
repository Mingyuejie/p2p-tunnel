using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;

namespace server.service.plugins
{
    public class HeartPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly ServerPluginHelper serverPluginHelper;

        public HeartPlugin(IClientRegisterCaching clientRegisterCache, ServerPluginHelper serverPluginHelper)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.serverPluginHelper = serverPluginHelper;
        }

        public void Excute(PluginParamWrap data)
        {
            HeartModel model = data.Wrap.Memory.DeBytes<HeartModel>();

            if (clientRegisterCache.Verify(model.SourceId, data))
            {
                clientRegisterCache.UpdateTime(model.SourceId);
            };
            if (data.ServerType == ServerType.UDP)
            {
                serverPluginHelper.SendOnly(new SendMessageWrap<HeartModel>
                {
                    Address = data.SourcePoint,
                    Data = new HeartModel { TargetId = model.SourceId, SourceId = -1 },
                    Type = ServerMessageTypes.REQUEST,
                    Path = data.Wrap.Path,
                    Code = ServerMessageResponeCodes.OK
                });
            }
            else
            {
                serverPluginHelper.SendOnlyTcp(new SendMessageWrap<HeartModel>
                {
                    TcpCoket = data.TcpSocket,
                    Data = new HeartModel { TargetId = model.SourceId, SourceId = -1 },
                    Type = ServerMessageTypes.REQUEST,
                    Path = data.Wrap.Path,
                    Code = ServerMessageResponeCodes.OK
                });
            }
        }
    }
}
