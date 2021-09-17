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
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        public ResetPlugin(IClientRegisterCache clientRegisterCache, ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
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
                        udpServer.SendReply(new SendMessageWrap<object>
                        {
                            Address = target.Address,
                            TcpCoket = null,
                            Data = model
                        });
                    }
                    else if (data.ServerType == ServerType.TCP)
                    {
                        tcpServer.SendReply(new SendMessageWrap<object>
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
