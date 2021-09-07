using common;
using common.cache;
using common.extends;
using server.model;
using server.service.model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace server.test
{
    class Program
    {
        static void Main(string[] args)
        {
            Config config = File.ReadAllText("appsettings.json").DeJson<Config>();

            UDPServer.Instance.Start(config.Udp);
            TCPServer.Instance.Start(config.Tcp);
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        //按分组id分组
                        var clients = ClientRegisterCache.Instance.GetAll()
                        .GroupBy(c => c.GroupId)
                        .Select(c => new KeyValuePair<string, IEnumerable<ClientsClientModel>>(c.Key, c.Select(c => new ClientsClientModel
                        {
                            Address = c.Address.Address.ToString(),
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
                                UDPServer.Instance.Send(new RecvQueueModel<IModelBase>
                                {
                                    Address = IPEndPoint.Parse(client.Address),
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

            _ = Console.ReadLine();
        }
    }
}
