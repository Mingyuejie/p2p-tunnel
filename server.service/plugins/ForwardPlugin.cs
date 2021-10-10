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

        public bool Excute(PluginParamWrap data)
        {
            ForwardModel model = data.Wrap.Content.DeBytes<ForwardModel>();

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
                        serverPluginHelper.SendOnly(new SendMessageWrap<byte[]>
                        {
                            Address = target.Address,
                            TcpCoket = null,
                            Data = model.Data,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId,
                            Code = data.Wrap.Code,
                            Type = data.Wrap.Type
                        });
                    }
                    else if (data.ServerType == ServerType.TCP)
                    {
                        serverPluginHelper.SendOnlyTcp(new SendMessageWrap<byte[]>
                        {
                            Address = target.Address,
                            TcpCoket = target.TcpSocket,
                            Data = model.Data,
                            Path = data.Wrap.Path,
                            RequestId = data.Wrap.RequestId,
                            Code = data.Wrap.Code,
                            Type = data.Wrap.Type
                        });
                    }
                }
            }

            return true;
        }
    }
}
