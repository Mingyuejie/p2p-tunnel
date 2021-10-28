using common;
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
            var tcpserver = obj.GetService<ITcpServer>();
            tcpserver.Start(obj.GetService<Config>().Tcp);
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

            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }

            return obj;
        }

        public static ServiceProvider UsePlugin(this ServiceProvider obj)
        {
            ServerPluginHelper serverPluginHelper = obj.GetService<ServerPluginHelper>();
            var types = AppDomain.CurrentDomain.GetAssemblies().SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)));
            foreach (var item in types)
            {
                serverPluginHelper.LoadPlugin(item, obj.GetService(item));
            }

            Loop(obj);

            return obj;
        }

        private static void Loop(ServiceProvider obj)
        {
            var clientRegisterCache = obj.GetService<IClientRegisterCaching>();
            ServerPluginHelper serverPluginHelper = obj.GetService<ServerPluginHelper>();
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        //按分组id分组
                        var clients = clientRegisterCache.GetAll()
                        .GroupBy(c => c.GroupId)
                        .Select(c => new KeyValuePair<string, IEnumerable<ClientsClientModel>>(c.Key, c.Select(c => new ClientsClientModel
                        {
                            Address = c.Address.Address.ToString(),
                            TcpSocket = c.TcpSocket,
                            Id = c.Id,
                            Name = c.Name,
                            Port = c.Address.Port,
                            TcpPort = c.TcpPort,
                            Mac = c.Mac
                        }).ToList()));

                        //每个分组
                        foreach (var group in clients)
                        {
                            //分组里的每个客户端
                            foreach (var client in group.Value)
                            {
                                serverPluginHelper.SendOnlyTcp(new SendMessageWrap<object>
                                {
                                    Address = IPEndPoint.Parse(client.Address),
                                    TcpCoket = client.TcpSocket,
                                    Data = new ClientsModel
                                    {
                                        Clients = group.Value
                                    },
                                    Path = "clients/excute"
                                });
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Instance.Debug($"发送广播客户端消息错误!{e.Message}");
                    }

                    System.Threading.Thread.Sleep(1000);
                }
            });
        }
    }
}
