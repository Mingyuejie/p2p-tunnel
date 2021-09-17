using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.cache;
using server.service.model;
using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace server.service.plugins
{
    public class RegisterPlugin : IPlugin
    {
        private readonly IClientRegisterCache clientRegisterCache;
        public RegisterPlugin(IClientRegisterCache clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public RegisterResultModel Excute(PluginParamWrap data)
        {
            try
            {
                RegisterModel model = data.Wrap.Content.DeBytes<RegisterModel>();
                if (data.ServerType == ServerType.UDP)
                {
                    if (clientRegisterCache.GetBySameGroup(model.GroupId, model.Name) == null)
                    {
                        RegisterCacheModel add = new()
                        {
                            Address = data.SourcePoint,
                            Name = model.Name,
                            LastTime = Helper.GetTimeStamp(),
                            TcpSocket = null,
                            TcpPort = 0,
                            OriginGroupId = model.GroupId,
                            LocalIps = model.LocalIps,
                            Mac = model.Mac,
                            LocalUdpPort = model.LocalUdpPort,
                            LocalTcpPort = model.LocalTcpPort,

                        };
                        long id = clientRegisterCache.Add(add, 0);
                        string origingid = add.OriginGroupId;
                        add.OriginGroupId = string.Empty;

                        return new RegisterResultModel
                        {
                            Id = id,
                            Ip = data.SourcePoint.Address.ToString(),
                            Port = data.SourcePoint.Port,
                            TcpPort = 0,
                            GroupId = origingid,
                            Mac = add.Mac,
                            LocalUdpPort = model.LocalUdpPort,
                            LocalTcpPort = model.LocalTcpPort,
                        };
                    }
                    else
                    {
                        data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "组中已存在同名客户端");
                    }
                }
                else if (data.ServerType == ServerType.TCP)
                {
                    var endpoint = IPEndPoint.Parse(data.TcpSocket.RemoteEndPoint.ToString());
                    var client = clientRegisterCache.Get(model.Id);
                    if (endpoint.Address.Equals(client.Address.Address) && clientRegisterCache.UpdateTcpInfo(model.Id, data.TcpSocket, endpoint.Port, model.GroupId))
                    {
                        return new RegisterResultModel
                        {
                            Id = model.Id,
                            Ip = data.SourcePoint.Address.ToString(),
                            Port = data.SourcePoint.Port,
                            TcpPort = endpoint.Port,
                            GroupId = model.GroupId,
                            Mac = model.Mac,
                            LocalUdpPort = model.LocalUdpPort,
                            LocalTcpPort = model.LocalTcpPort,
                        };
                    }
                    else
                    {
                        data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "TCP注册失败");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex + "");
                data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, ex.Message);
            }

            return null;
        }
    }
}
