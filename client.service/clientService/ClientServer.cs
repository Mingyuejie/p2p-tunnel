using client.service.clientService.plugins;
using client.service.config;
using client.service.p2pPlugins.fileServer;
using client.service.p2pPlugins.forward.tcp;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.register;
using client.service.serverPlugins.register.client;
using common;
using common.extends;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.clientService
{
    public interface IClientServer
    {
        public void Start();
        public void LoadPlugins(Assembly[] assemblys);
    }

    public class ClientServer : IClientServer
    {
        private readonly ConcurrentDictionary<Guid, IWebSocketConnection> websockets = new();

        private readonly Dictionary<string, Tuple<object, MethodInfo>> plugins = new();

        private readonly Config config;
        private readonly TcpForwardHelper tcpForwardHelper;
        private readonly FileServerHelper fileServerHelper;
        private readonly ClientsHelper clientsHelper;
        private readonly RegisterState registerState;
        private readonly ServiceProvider serviceProvider;

        public ClientServer(Config config, TcpForwardHelper tcpForwardHelper,
            FileServerHelper fileServerHelper, RegisterState registerState,
            ClientsHelper clientsHelper, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.tcpForwardHelper = tcpForwardHelper;
            this.fileServerHelper = fileServerHelper;
            this.registerState = registerState;
            this.clientsHelper = clientsHelper;
            this.serviceProvider = serviceProvider;

            Notify();
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            var types = assemblys
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IClientServicePlugin)));
            foreach (var item in types)
            {
                string path = item.Name.Replace("Plugin", "");
                object obj = serviceProvider.GetService(item);
                foreach (var method in item.GetMethods())
                {
                    string key = $"{path}/{method.Name}".ToLower();
                    if (!plugins.ContainsKey(key))
                    {
                        plugins.TryAdd(key, new Tuple<object, MethodInfo>(obj, method));
                    }
                }
            }
        }

        public void Start()
        {
            WebSocketServer server = new($"ws://{config.Websocket.BindIp}:{config.Websocket.Port}");
            server.RestartAfterListenError = true;
            FleckLog.LogAction = (level, message, ex) =>
            {
                switch (level)
                {
                    case LogLevel.Debug:
                        //Logger.Instance.Info(message);
                        break;
                    case LogLevel.Info:
                        //Logger.Instance.Info(message);
                        break;
                    case LogLevel.Warn:
                        Logger.Instance.Debug(message);
                        break;
                    case LogLevel.Error:
                        Logger.Instance.Error(message);
                        break;
                    default:
                        break;
                }
            };

            server.Start(socket =>
            {
                socket.OnClose = () =>
                {
                    _ = websockets.TryRemove(socket.ConnectionInfo.Id, out _);
                };
                socket.OnOpen = () =>
                {
                    _ = websockets.TryAdd(socket.ConnectionInfo.Id, socket);
                    NotifyVersion(socket);
                };
                socket.OnMessage = message =>
                {
                    OnMessage(socket, message);
                };
            });

            Logger.Instance.Info("本地服务已启动...");
        }

        private void OnMessage(IWebSocketConnection socket, string message)
        {
            _ = Task.Run(() =>
            {
                ClientServiceMessageWrap model = message.DeJson<ClientServiceMessageWrap>();
                model.Path = model.Path.ToLower();
                if (plugins.ContainsKey(model.Path))
                {
                    var plugin = plugins[model.Path];
                    try
                    {
                        var param = new ClientServicePluginExcuteWrap
                        {
                            Socket = socket,
                            RequestId = model.RequestId,
                            Content = model.Content,
                            Websockets = websockets,
                            Path = model.Path
                        };
                        object resultAsync = plugin.Item2.Invoke(plugin.Item1, new object[] { param });

                        object resultObject = null;
                        if (resultAsync is Task task)
                        {
                            task.Wait();
                            if (resultAsync is Task<object> task1)
                            {
                                resultObject = task1.Result;
                            }
                        }
                        else
                        {
                            resultObject = resultAsync;
                        }
                        param.Socket.Send(new ClientServiceMessageResponseWrap
                        {
                            Content = param.Code == 0 ? resultObject : param.ErrorMessage,
                            RequestId = param.RequestId,
                            Path = param.Path,
                            Code = param.Code
                        }.ToJson());
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Debug(ex + "");
                        socket.Send(new ClientServiceMessageResponseWrap
                        {
                            Content = ex.Message,
                            RequestId = model.RequestId,
                            Path = model.Path,
                            Code = -1
                        }.ToJson());
                    }
                }
            });
        }

        private void Notify()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    foreach (var connection in websockets.Values)
                    {
                        connection.Send(new ClientServiceMessageResponseWrap
                        {
                            Path = "merge",
                            RequestId = 0,
                            Code = 0,
                            Content = new List<ClientServiceMessageResponseWrap> {
                                new ClientServiceMessageResponseWrap
                                {
                                    Content = clientsHelper.Clients,
                                    RequestId = 0,
                                    Path = "clients/list",
                                    Code = 0
                                },new ClientServiceMessageResponseWrap
                                {
                                    Content = new RegisterInfo
                                    {
                                        ClientConfig = config.Client,
                                        ServerConfig = config.Server,
                                        LocalInfo = registerState.LocalInfo,
                                        RemoteInfo = registerState.RemoteInfo,
                                    },
                                    RequestId = 0,
                                    Path = "register/info",
                                },new ClientServiceMessageResponseWrap
                                {
                                    Content = tcpForwardHelper.Mappings,
                                    RequestId = 0,
                                    Path = "tcpforward/list",
                                    Code = 0
                                },
                                 new ClientServiceMessageResponseWrap
                                {
                                    Content = config.FileServer,
                                    RequestId = 0,
                                    Path = "fileserver/info",
                                    Code = 0
                                },
                                new ClientServiceMessageResponseWrap
                                {
                                    Content = fileServerHelper.GetOnlineList(),
                                    RequestId = 0,
                                    Path = "fileserver/online",
                                    Code = 0
                                }
                            }
                        }.ToJson());
                    }
                    System.Threading.Thread.Sleep(300);
                }
            });
        }

        private void NotifyVersion(IWebSocketConnection connection)
        {
            connection.Send(new ClientServiceMessageResponseWrap
            {
                Content = new
                {
                    Local = System.IO.File.ReadAllText("version.txt"),
                    Remote = GetVersion()
                },
                RequestId = 0,
                Path = "system/version",
                Code = 0
            }.ToJson());
        }

        private string GetVersion()
        {
            try
            {
                return new HttpClient().GetStringAsync($"https://gitee.com/snltty/p2p-tunnel/raw/master/client.service/version.txt?t={Helper.GetTimeStamp()}").Result;
            }
            catch (Exception)
            {
            }
            return string.Empty;
        }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddClientServer(this ServiceCollection obj)
        {
            obj.AddClientServer(AppDomain.CurrentDomain.GetAssemblies());
            obj.AddSingleton<IClientServer, ClientServer>();
            obj.AddUpnpPlugin();

            return obj;
        }
        public static ServiceCollection AddClientServer(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IClientServicePlugin)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }
            return obj;
        }

        public static ServiceProvider UseClientServer(this ServiceProvider obj)
        {
            obj.UseClientServer(AppDomain.CurrentDomain.GetAssemblies());
            obj.GetService<IClientServer>().Start();
            obj.UseUpnpPlugin();
            return obj;
        }

        public static ServiceProvider UseClientServer(this ServiceProvider obj, Assembly[] assemblys)
        {
            obj.GetService<IClientServer>().LoadPlugins(assemblys);
            return obj;
        }
    }
}
