using client.service.clientService.plugins;
using client.service.config;
using client.service.p2pPlugins.plugins.fileServer;
using client.service.p2pPlugins.plugins.forward.tcp;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.register;
using common;
using common.extends;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FileServerPlugin = client.service.clientService.plugins.FileServerPlugin;
using TcpForwardPlugin = client.service.clientService.plugins.TcpForwardPlugin;

namespace client.service.clientService
{
    public interface IClientServer
    {
        public void Start();
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
       
        public ClientServer(Config config, TcpForwardHelper tcpForwardHelper, FileServerHelper fileServerHelper, RegisterState registerState, ClientsHelper clientsHelper)
        {
            this.config = config;
            this.tcpForwardHelper = tcpForwardHelper;
            this.fileServerHelper = fileServerHelper;
            this.registerState = registerState;
            this.clientsHelper = clientsHelper;

            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IClientServicePlugin)));
            foreach (var item in types)
            {
                string path = item.Name.Replace("Plugin", "");
                object obj = Program.serviceProvider.GetService(item);
                foreach (var method in item.GetMethods())
                {
                    string key = $"{path}/{method.Name}".ToLower();
                    if (!plugins.ContainsKey(key))
                    {
                        plugins.TryAdd(key, new Tuple<object, MethodInfo>(obj, method));
                    }
                }
            }

            Notify();
        }

        public void Start()
        {
            WebSocketServer server = new($"ws://{config.Websocket.Ip}:{config.Websocket.Port}");
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
                        string resultString = string.Empty;
                        if (resultObject != null)
                        {
                            if (resultObject is string)
                            {
                                resultString = resultObject as string;
                            }
                            else
                            {
                                resultString = resultObject.ToJson();
                            }
                        }

                        param.Socket.Send(new ClientServiceMessageWrap
                        {
                            Content = param.Code == 0 ? resultString : param.ErrorMessage,
                            RequestId = param.RequestId,
                            Path = param.Path,
                            Code = param.Code
                        }.ToJson());

                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Debug(ex + "");
                        socket.Send(new ClientServiceMessageWrap
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
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = clientsHelper.Clients.ToJson(),
                            RequestId = 0,
                            Path = "clients/list",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = new RegisterInfo
                            {
                                ClientConfig = config.Client,
                                ServerConfig = config.Server,
                                LocalInfo = registerState.LocalInfo,
                                RemoteInfo = registerState.RemoteInfo,
                            }.ToJson(),
                            RequestId = 0,
                            Path = "register/info",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = tcpForwardHelper.Mappings.ToJson(),
                            RequestId = 0,
                            Path = "tcpforward/list",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = config.FileServer.ToJson(),
                            RequestId = 0,
                            Path = "fileserver/info",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = fileServerHelper.GetOnlineList().ToJson(),
                            RequestId = 0,
                            Path = "fileserver/online",
                            Code = 0
                        }.ToJson());
                    }
                    System.Threading.Thread.Sleep(500);
                }
            });
        }

        private void NotifyVersion(IWebSocketConnection connection)
        {
            connection.Send(new ClientServiceMessageWrap
            {
                Content = new
                {
                    Local = System.IO.File.ReadAllText("version.txt"),
                    Remote = GetVersion()
                }.ToJson(),
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
            obj.AddSingleton<ClientsPlugin>();
            obj.AddSingleton<ConfigPlugin>();
            obj.AddSingleton<FileServerPlugin>();
            obj.AddSingleton<RegisterPlugin>();
            obj.AddSingleton<ResetPlugin>();
            obj.AddSingleton<TcpForwardPlugin>();
            obj.AddSingleton<WakeUpPlugin>();

            obj.AddSingleton<IClientServer, ClientServer>();

            obj.AddUpnpPlugin();

            return obj;
        }
        public static ServiceProvider UseClientServer(this ServiceProvider obj)
        {
            obj.GetService<IClientServer>().Start();

            obj.UseUpnpPlugin();

            return obj;
        }
    }
}
