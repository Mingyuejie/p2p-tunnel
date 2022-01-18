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

        public async Task<RegisterResultModel> Execute(IConnection connection)
        {
            await Task.Yield();
            try
            {
                RegisterModel model = connection.ReceiveRequestWrap.Memory.DeBytes<RegisterModel>();
                if (connection.ServerType == ServerType.UDP)
                {
                    if (clientRegisterCache.GetBySameGroup(model.GroupId, model.Name) == null)
                    {
                        RegisterCacheModel add = new()
                        {
                            UdpConnection = connection,
                            Name = model.Name,
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
                        connection.ConnectId = add.Id;

                        return new RegisterResultModel
                        {
                            Id = add.Id,
                            Ip = connection.UdpAddress.Address.ToString(),
                            Port = connection.UdpAddress.Port,
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
                else if (connection.ServerType == ServerType.TCP)
                {
                    IPEndPoint endpoint = connection.TcpSocket.RemoteEndPoint as IPEndPoint;
                    if (!clientRegisterCache.Get(model.Id, out RegisterCacheModel client) || !endpoint.Address.Equals(client.UdpConnection.UdpAddress.Address))
                    {
                        return new RegisterResultModel
                        {
                            Code = RegisterResultModel.RegisterResultCodes.VERIFY
                        };
                    }
                    else
                    {
                        connection.ConnectId = client.Id;
                        clientRegisterCache.UpdateTcpInfo(new RegisterCacheUpdateModel
                        {
                            Id = client.Id,
                            TcpConnection = connection,
                            GroupId = model.GroupId,
                            LocalTcpPort = model.LocalTcpPort,
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

        public async Task Notify(IConnection connection)
        {
            await clientRegisterCache.Notify(connection);
        }
    }
}
