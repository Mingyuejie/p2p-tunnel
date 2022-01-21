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
        public static ServiceCollection AddTcpServer(this ServiceCollection services)
        {
            services.AddSingleton<ITcpServer, TCPServer>();
            return services;
        }
        public static ServiceCollection AddUdpServer(this ServiceCollection services)
        {
            services.AddSingleton<IUdpServer, UDPServer>();
            return services;
        }

        public static ServiceProvider UseTcpServer(this ServiceProvider services)
        {
            var config = services.GetService<Config>();
            var server = services.GetService<ITcpServer>();
            server.SetBufferSize(config.TcpBufferSize);
            server.Start(config.Tcp, ip: IPAddress.Any);

            Logger.Instance.Info("TCP服务已开启");

            return services;
        }
        public static ServiceProvider UseUdpServer(this ServiceProvider services)
        {
            services.GetService<IUdpServer>().Start(services.GetService<Config>().Udp);

            Logger.Instance.Info("UDP服务已开启");

            return services;
        }

        public static ServiceCollection AddPlugin(this ServiceCollection services)
        {
            services.AddSingleton<IClientRegisterCaching, ClientRegisterCaching>();
            services.AddSingleton<ServerPluginHelper>();

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), typeof(IPlugin)))
            {
                services.AddSingleton(item);
            }

            return services;
        }

        public static ServiceProvider UsePlugin(this ServiceProvider services)
        {
            ServerPluginHelper serverPluginHelper = services.GetService<ServerPluginHelper>();
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), typeof(IPlugin)))
            {
                serverPluginHelper.LoadPlugin(item, services.GetService(item));
            }

            Loop(services);

            return services;
        }

        private static void Loop(ServiceProvider services)
        {
            IClientRegisterCaching clientRegisterCache = services.GetService<IClientRegisterCaching>();
            ServerPluginHelper serverPluginHelper = services.GetService<ServerPluginHelper>();

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
