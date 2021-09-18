using common.extends;
using server.model;
using server.plugin;
using server.service.cache;
using server.service.model;

namespace server.service.plugins
{
    public class ResetPlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        private readonly ServerPluginHelper serverPluginHelper;
        public ResetPlugin(IClientRegisterCache clientRegisterCache, ServerPluginHelper serverPluginHelper)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.serverPluginHelper = serverPluginHelper;
        }

        public bool Excute(PluginParamWrap data)
        {
            ResetModel model = data.Wrap.Content.DeBytes<ResetModel>();

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
                            Data = model
                        });
                    }
                    else if (data.ServerType == ServerType.TCP)
                    {
                        serverPluginHelper.SendOnlyTcp(new SendMessageWrap<object>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model
                        });
                    }
                }
            }

            return true;
        }
    }
}
