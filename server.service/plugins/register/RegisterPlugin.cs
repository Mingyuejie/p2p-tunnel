using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System;
using System.Net;

namespace server.service.plugins.register
{
    public class RegisterPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public RegisterPlugin(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public RegisterResultModel Excute(PluginParamWrap data)
        {
            try
            {
                RegisterModel model = data.Wrap.Memory.DeBytes<RegisterModel>();
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
                            Id = 0
                        };
                        clientRegisterCache.Add(add);
                        string origingid = add.OriginGroupId;
                        add.OriginGroupId = string.Empty;

                        return new RegisterResultModel
                        {
                            Id = add.Id,
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
                    if (client == null || !endpoint.Address.Equals(client.Address.Address))
                    {
                        data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, "TCP注册失败");
                    }
                    else
                    {
                        clientRegisterCache.UpdateTcpInfo(new RegisterCacheUpdateModel
                        {
                            Id = client.Id,
                            TcpSocket = data.TcpSocket,
                            TcpPort = endpoint.Port,
                            GroupId = model.GroupId
                        });
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
