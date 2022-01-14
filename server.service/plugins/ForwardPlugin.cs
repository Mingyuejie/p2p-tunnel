using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.service.plugins
{
    /// <summary>
    /// P2P数据转发
    /// </summary>
    public class ForwardPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        private readonly ServerPluginHelper serverPluginHelper;
        public ForwardPlugin(IClientRegisterCaching clientRegisterCache, ServerPluginHelper serverPluginHelper)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.serverPluginHelper = serverPluginHelper;
        }

        public async Task Execute(PluginParamWrap data)
        {
            ForwardModel model = data.Wrap.Memory.DeBytes<ForwardModel>();

            //A已注册
            if (clientRegisterCache.Get(data.Connection.ConnectId, out RegisterCacheModel source))
            {
                //B已注册
                if (clientRegisterCache.Get(model.ToId, out RegisterCacheModel target))
                {
                    //是否在同一个组
                    if (source.GroupId == target.GroupId)
                    {
                       await  serverPluginHelper.SendOnly(new MessageRequestParamsWrap<byte[]>
                        {
                            Connection = data.Connection,
                            Data = model.Data,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId
                        });
                    }
                }
            }
        }
    }
}
