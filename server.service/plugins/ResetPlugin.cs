using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System.Threading.Tasks;

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

        public async Task<bool> Execute(PluginParamWrap data)
        {
            ResetModel model = data.Wrap.Memory.DeBytes<ResetModel>();

            //A已注册
            if (clientRegisterCache.Get(data.Connection.ConnectId, out RegisterCacheModel source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheModel target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                        return (await serverPluginHelper.SendReply(new MessageRequestParamsWrap<ResetModel>
                        {
                            Connection = data.Connection.ServerType == ServerType.UDP ? target.UdpConnection : target.TcpConnection,
                            Data = model,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId
                        })).Code == MessageResponeCode.OK;
                    }
                }
            }

            return false;
        }
    }
}
