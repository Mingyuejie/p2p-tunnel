using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System;
using System.Net;
using System.Threading.Tasks;

namespace server.service.plugins.register
{
    public class RegisterPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public RegisterPlugin(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public async Task<RegisterResultModel> Execute(PluginParamWrap data)
        {
            await Task.Yield();
            try
            {
                RegisterModel model = data.Wrap.Memory.DeBytes<RegisterModel>();
                if (data.Connection.ServerType == ServerType.UDP)
                {
                    if (clientRegisterCache.GetBySameGroup(model.GroupId, model.Name) == null)
                    {
                        RegisterCacheModel add = new()
                        {
                            UdpConnection = data.Connection,
                            Name = model.Name,
                            LastTime = Helper.GetTimeStamp(),
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
                        data.Connection.ConnectId = add.Id;

                        return new RegisterResultModel
                        {
                            Id = add.Id,
                            Ip = data.Connection.UdpAddress.Address.ToString(),
                            Port = data.Connection.UdpAddress.Port,
                            TcpPort = 0,
                            GroupId = origingid
                        };
                    }
                    else
                    {
                        return new RegisterResultModel
                        {
                            Code = RegisterResultModel.RegisterResultCodes.SAME_NAMES
                        };
                    }
                }
                else if (data.Connection.ServerType == ServerType.TCP)
                {
                    IPEndPoint endpoint = data.Connection.TcpSocket.RemoteEndPoint as IPEndPoint;
                    if (!clientRegisterCache.Get(model.Id, out RegisterCacheModel client) || !endpoint.Address.Equals(client.UdpConnection.UdpAddress.Address))
                    {
                        return new RegisterResultModel
                        {
                            Code = RegisterResultModel.RegisterResultCodes.VERIFY
                        };
                    }
                    else
                    {
                        data.Connection.ConnectId = client.Id;
                        clientRegisterCache.UpdateTcpInfo(new RegisterCacheUpdateModel
                        {
                            Id = client.Id,
                            TcpConnection = data.Connection,
                            GroupId = model.GroupId
                        });
                        return new RegisterResultModel
                        {
                            Id = model.Id,
                            Ip = client.UdpConnection.UdpAddress.Address.ToString(),
                            Port = client.UdpConnection.UdpAddress.Port,
                            TcpPort = endpoint.Port,
                            GroupId = model.GroupId
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
                return new RegisterResultModel
                {
                    Code = RegisterResultModel.RegisterResultCodes.UNKNOW
                };
            }

            return new RegisterResultModel
            {
                Code = RegisterResultModel.RegisterResultCodes.UNKNOW
            };
        }

        public void Notify(PluginParamWrap data)
        {
            clientRegisterCache.Notify(data.Connection);
        }
    }
}
