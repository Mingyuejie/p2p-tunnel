﻿using common;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
using server.service.cache;
using server.service.model;
using server.service.plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
            var clientRegisterCache = obj.GetService<IClientRegisterCache>();
            var tcpserver = obj.GetService<ITcpServer>();
            tcpserver.Start(config.Tcp);

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
                            TcpPort = c.TcpPort
                        }).ToList()));

                        //每个分组
                        foreach (var group in clients)
                        {
                            //分组里的每个客户端
                            foreach (var client in group.Value)
                            {
                                tcpserver.Send(new RecvQueueModel<IModelBase>
                                {
                                    Address = IPEndPoint.Parse(client.Address),
                                    TcpCoket = client.TcpSocket,
                                    Data = new ClientsModel
                                    {
                                        Clients = group.Value
                                    }
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
            obj.AddSingleton<IClientRegisterCache, ClientRegisterCache>();
            obj.AddSingleton<ExitPlugin>();
            obj.AddSingleton<HeartPlugin>();
            obj.AddSingleton<RegisterPlugin>();
            obj.AddSingleton<ResetPlugin>();
            obj.AddSingleton<PunchHolePlugin>();

            obj.AddSingleton<ConnectClientPlugin>();
            obj.AddSingleton<ConnectClientReversePlugin>();
            obj.AddSingleton<ConnectClientStep1ResultPlugin>();
            obj.AddSingleton<ConnectClientStep2RetryPlugin>();
            obj.AddSingleton<ConnectClientStep2FailPlugin>();
            obj.AddSingleton<ConnectClientStep2StopPlugin>();
            return obj;
        }

        public static ServiceProvider UsePlugin(this ServiceProvider obj)
        {
            Plugin.LoadPlugin(obj.GetService<ExitPlugin>());
            Plugin.LoadPlugin(obj.GetService<HeartPlugin>());
            Plugin.LoadPlugin(obj.GetService<RegisterPlugin>());
            Plugin.LoadPlugin(obj.GetService<ResetPlugin>());
            Plugin.LoadPlugin(obj.GetService<PunchHolePlugin>());

            Plugin.LoadPlugin(obj.GetService<ConnectClientPlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientReversePlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep1ResultPlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep2RetryPlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep2FailPlugin>());
            Plugin.LoadPlugin(obj.GetService<ConnectClientStep2StopPlugin>());

            return obj;
        }
    }
}
