using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;

namespace server.service.plugins
{
    public class ResetPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly ServerPluginHelper serverPluginHelper;
        public ResetPlugin(IClientRegisterCaching clientRegisterCache, ServerPluginHelper serverPluginHelper)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.serverPluginHelper = serverPluginHelper;
        }

        public bool Excute(PluginParamWrap data)
        {
            ResetModel model = data.Wrap.Memory.DeBytes<ResetModel>();

            if (!clientRegisterCache.Verify(model.Id, data)) return false;

            //A已注册
            RegisterCacheModel source = clientRegisterCache.Get(model.Id);
            if (source != null)
            {
                //B已注册
                RegisterCacheModel target = clientRegisterCache.Get(model.ToId);
                if (target != null)
                {
                    //是否在同一个组
                    if (source.GroupId != target.GroupId)
                    {
                        return false;
                    }

                    if (data.ServerType == ServerType.UDP)
                    {
                        serverPluginHelper.SendOnly(new SendMessageWrap<object>
                        {
                            Address = target.Address,
                            TcpCoket = null,
                            Data = model,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId,
                            Code = ServerMessageResponeCodes.OK
                        });
                    }
                    else if (data.ServerType == ServerType.TCP)
                    {
                        serverPluginHelper.SendOnlyTcp(new SendMessageWrap<object>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId,
                            Code = ServerMessageResponeCodes.OK
                        });
                    }
                }
            }

            return true;
        }
    }
}
