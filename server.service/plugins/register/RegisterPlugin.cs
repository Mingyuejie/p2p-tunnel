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

        public RegisterResultModel Execute(IConnection connection)
        {
            RegisterModel model = connection.ReceiveRequestWrap.Memory.DeBytes<RegisterModel>();

            return connection.ServerType switch
            {
                ServerType.UDP => Udp(connection, model),
                ServerType.TCP => Tcp(connection, model),
                _ => new RegisterResultModel { Code = RegisterResultModel.RegisterResultCodes.UNKNOW }
            };
        }
        private RegisterResultModel Udp(IConnection connection, RegisterModel model)
        {
            if (clientRegisterCache.GetBySameGroup(model.GroupId, model.Name) != null)
            {
                return new RegisterResultModel { Code = RegisterResultModel.RegisterResultCodes.SAME_NAMES };
            }
            RegisterCacheModel client = new()
            {
                Name = model.Name,
                OriginGroupId = model.GroupId,
                LocalIps = model.LocalIps,
                Mac = model.Mac,
                Id = 0
            };
            clientRegisterCache.Add(client);

            client.UpdateUdpInfo(new RegisterCacheUpdateUdpModel { Connection = connection });
            client.AddTunnel(new TunnelRegisterCacheModel
            {
                Port = connection.UdpAddress.Port,
                LocalPort = model.LocalUdpPort,
                Servertype = ServerType.UDP,
                TunnelName = "udp",
                IsDefault = true,
            });

            return new RegisterResultModel
            {
                Id = client.Id,
                Ip = connection.UdpAddress.Address.ToString(),
                Port = connection.UdpAddress.Port,
                TcpPort = 0,
                GroupId = client.OriginGroupId
            };
        }
        private RegisterResultModel Tcp(IConnection connection, RegisterModel model)
        {
            if (!clientRegisterCache.Get(model.Id, out RegisterCacheModel client))
            {
                return new RegisterResultModel { Code = RegisterResultModel.RegisterResultCodes.VERIFY };
            }

            client.UpdateTcpInfo(new RegisterCacheUpdateTcpModel
            {
                Connection = connection
            });
            client.AddTunnel(new TunnelRegisterCacheModel
            {
                Port = connection.TcpAddress.Port,
                LocalPort = model.LocalTcpPort,
                Servertype = ServerType.TCP,
                TunnelName = "tcp",
                IsDefault = true,
            });

            return new RegisterResultModel
            {
                Id = model.Id,
                Ip = client.UdpConnection.UdpAddress.Address.ToString(),
                Port = client.UdpConnection.UdpAddress.Port,
                TcpPort = connection.TcpAddress.Port,
                GroupId = model.GroupId
            };
        }

        public async Task Notify(IConnection connection)
        {
            await clientRegisterCache.Notify(connection);
        }

        public TunnelRegisterResult Tunnel(IConnection connection)
        {
            IPEndPoint endpoint = connection.TcpSocket.RemoteEndPoint as IPEndPoint;
            if (!clientRegisterCache.Get(connection.ConnectId, out RegisterCacheModel client))
            {
                return new TunnelRegisterResult { Code = TunnelRegisterResult.TunnelRegisterResultCodes.VERIFY };
            }

            TunnelRegisterModel model = connection.ReceiveRequestWrap.Memory.DeBytes<TunnelRegisterModel>();

            int port = connection.ServerType == ServerType.UDP ? connection.UdpAddress.Port : endpoint.Port;
            TunnelRegisterCacheModel cache = new TunnelRegisterCacheModel
            {
                TunnelName = model.TunnelName,
                Port = port,
                LocalPort = model.LocalPort,
                Servertype = connection.ServerType
            };
            if (client.TunnelExists(cache))
            {
                return new TunnelRegisterResult { Code = TunnelRegisterResult.TunnelRegisterResultCodes.SAME_NAMES };
            }

            client.AddTunnel(cache);

            return new TunnelRegisterResult { Code = TunnelRegisterResult.TunnelRegisterResultCodes.OK };
        }
    }
}
