using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.servers.defaults;
using server.service.messengers.register.caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace server.service
{
    static class ServiceCollectionExtends
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

        public static ServiceCollection AddMessenger(this ServiceCollection services)
        {
            services.AddSingleton<IClientRegisterCaching, ClientRegisterCaching>();
            services.AddSingleton<MessengerResolver>();
            services.AddSingleton<MessengerSender>();
            
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), typeof(IMessenger)))
            {
                services.AddSingleton(item);
            }

            return services;
        }

        public static ServiceProvider UseMessenger(this ServiceProvider services)
        {
            MessengerResolver messenger = services.GetService<MessengerResolver>();
            MessengerSender sender = services.GetService<MessengerSender>();
            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), typeof(IMessenger)))
            {
                messenger.LoadMessenger(item, services.GetService(item));
            }

            Loop(services);

            return services;
        }

        private static void Loop(ServiceProvider services)
        {
            IClientRegisterCaching clientRegisterCache = services.GetService<IClientRegisterCaching>();
            MessengerResolver messengerResolver = services.GetService<MessengerResolver>();
            MessengerSender messengerSender = services.GetService<MessengerSender>();

            clientRegisterCache.OnChanged.SubAsync(async (string group) =>
            {
                List<ClientsClientInfo> clients = clientRegisterCache.GetAll().Where(c => c.GroupId == group && c.TcpConnection != null).Select(c => new ClientsClientInfo
                {
                    Address = c.UdpConnection.Address.Address.ToString(),
                    TcpConnection = c.TcpConnection,
                    UdpConnection = c.UdpConnection,
                    Id = c.Id,
                    Name = c.Name,
                    Port = c.UdpConnection.Address.Port,
                    TcpPort = c.TcpConnection.Address.Port,
                    Mac = c.Mac
                }).ToList();
                if (clients.Any())
                {
                    byte[] bytes = new ClientsInfo
                    {
                        Clients = clients
                    }.ToBytes();
                    foreach (ClientsClientInfo client in clients)
                    {
                        await messengerSender.SendOnly(new MessageRequestParamsInfo<byte[]>
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
