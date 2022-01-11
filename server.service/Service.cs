using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.achieves.defaults;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace server.service
{
    static class Service
    {
        public static ServiceCollection AddTcpServer(this ServiceCollection obj)
        {
            obj.AddSingleton<ITcpServer, TCPServer>();
            return obj;
        }
        public static ServiceCollection AddUdpServer(this ServiceCollection obj)
        {
            obj.AddSingleton<IUdpServer, UDPServer>();
            return obj;
        }

        public static ServiceProvider UseTcpServer(this ServiceProvider obj)
        {
            var config = obj.GetService<Config>();
            var server = obj.GetService<ITcpServer>();
            server.SetBufferSize(config.TcpBufferSize);
            server.Start(config.Tcp, ip: IPAddress.Any);

            Logger.Instance.Info("TCP服务已开启");

            return obj;
        }
        public static ServiceProvider UseUdpServer(this ServiceProvider obj)
        {
            obj.GetService<IUdpServer>().Start(obj.GetService<Config>().Udp);

            Logger.Instance.Info("UDP服务已开启");

            return obj;
        }

        public static ServiceCollection AddPlugin(this ServiceCollection obj)
        {
            obj.AddSingleton<IClientRegisterCaching, ClientRegisterCaching>();
            obj.AddSingleton<ServerPluginHelper>();

            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (Type item in types)
            {
                obj.AddSingleton(item);
            }

            return obj;
        }

        public static ServiceProvider UsePlugin(this ServiceProvider obj)
        {
            ServerPluginHelper serverPluginHelper = obj.GetService<ServerPluginHelper>();
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (Type item in types)
            {
                serverPluginHelper.LoadPlugin(item, obj.GetService(item));
            }

            Loop(obj);

            return obj;
        }

        private static void Loop(ServiceProvider obj)
        {
            IClientRegisterCaching clientRegisterCache = obj.GetService<IClientRegisterCaching>();
            ServerPluginHelper serverPluginHelper = obj.GetService<ServerPluginHelper>();

            clientRegisterCache.OnChanged.SubAsync(async (string group) =>
            {
                List<ClientsClientModel> clients = clientRegisterCache.GetAll().Where(c => c.GroupId == group && c.TcpConnection != null).Select(c => new ClientsClientModel
                {
                    Address = c.UdpConnection.UdpAddress.Address.ToString(),
                    TcpConnection = c.TcpConnection,
                    UdpConnection = c.UdpConnection,
                    Id = c.Id,
                    Name = c.Name,
                    Port = c.UdpConnection.UdpAddress.Port,
                    TcpPort = (c.TcpConnection.TcpSocket.RemoteEndPoint as IPEndPoint).Port,
                    Mac = c.Mac
                }).ToList();
                if (clients.Any())
                {
                    byte[] bytes = new ClientsModel
                    {
                        Clients = clients
                    }.ToBytes();
                    foreach (ClientsClientModel client in clients)
                    {
                        await serverPluginHelper.SendOnly(new MessageRequestParamsWrap<byte[]>
                        {
                            Connection = client.TcpConnection,
                            Data = bytes,
                            Path = "clients/Execute"
                        });
                    }
                }
            });
        }
    }
}
