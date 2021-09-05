using client.service.clientService.plugins;
using client.service.p2pPlugins.plugins.fileServer;
using client.service.p2pPlugins.plugins.forward.tcp;
using common;
using Fleck;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.clientService
{
    public class ClientServer
    {
        private static readonly Lazy<ClientServer> lazy = new(() => new ClientServer());
        public static ClientServer Instance => lazy.Value;

        private readonly ConcurrentDictionary<Guid, IWebSocketConnection> websockets = new();

        private readonly Dictionary<string, Tuple<object, MethodInfo>> plugins = new();

        private ClientServer()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IClientServicePlugin)));
            foreach (var item in types)
            {
                string path = item.Name.Replace("Plugin", "");
                object obj = Activator.CreateInstance(item);
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

        public void Start(string ip, int port)
        {
            WebSocketServer server = new($"ws://{ip}:{port}");
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
                        Logger.Instance.Info(message);
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
                    Task.Run(() =>
                    {
                        ClientServiceMessageWrap model = Helper.DeJsonSerializer<ClientServiceMessageWrap>(message);
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
                                    Path = model.Path,
                                    Callback = (param, returnResult) =>
                                    {
                                        string result = string.Empty;
                                        if (returnResult != null)
                                        {
                                            if (returnResult is string)
                                            {
                                                result = returnResult as string;
                                            }
                                            else
                                            {
                                                result = Helper.JsonSerializer(returnResult);
                                            }
                                        }
                                        param.Socket.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
                                        {
                                            Content = param.Code == 0 ? result : param.Message,
                                            RequestId = param.RequestId,
                                            Path = param.Path,
                                            Code = param.Code
                                        }));
                                    }
                                };
                                plugin.Item2.Invoke(plugin.Item1, new object[] { param });
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Info(ex + "");
                                socket.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
                                {
                                    Content = ex.Message,
                                    RequestId = model.RequestId,
                                    Path = model.Path,
                                    Code = -1
                                }));
                            }
                        }
                    });
                };
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
                        connection.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
                        {
                            Content = Helper.JsonSerializer(AppShareData.Instance.Clients.Values.ToList()),
                            RequestId = 0,
                            Path = "clients/list",
                            Code = 0
                        }));
                        connection.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
                        {
                            Content = Helper.JsonSerializer(new RegisterInfo
                            {
                                ClientConfig = AppShareData.Instance.ClientConfig,
                                ServerConfig = AppShareData.Instance.ServerConfig,
                                LocalInfo = AppShareData.Instance.LocalInfo,
                                RemoteInfo = AppShareData.Instance.RemoteInfo,
                            }),
                            RequestId = 0,
                            Path = "register/info",
                            Code = 0
                        }));
                        connection.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
                        {
                            Content = Helper.JsonSerializer(TcpForwardHelper.Instance.Mappings),
                            RequestId = 0,
                            Path = "tcpforward/list",
                            Code = 0
                        }));
                        connection.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
                        {
                            Content = Helper.JsonSerializer(AppShareData.Instance.FileServerConfig),
                            RequestId = 0,
                            Path = "fileserver/info",
                            Code = 0
                        }));
                        connection.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
                        {
                            Content = Helper.JsonSerializer(FileServerHelper.Instance.GetOnlineList()),
                            RequestId = 0,
                            Path = "fileserver/online",
                            Code = 0
                        }));
                    }
                    System.Threading.Thread.Sleep(500);
                }
            });
        }

        private void NotifyVersion(IWebSocketConnection connection)
        {
            connection.Send(Helper.JsonSerializer(new ClientServiceMessageWrap
            {
                Content = Helper.JsonSerializer(new
                {
                    Local = System.IO.File.ReadAllText("version.txt"),
                    Remote = GetVersion()
                }),
                RequestId = 0,
                Path = "system/version",
                Code = 0
            }));
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
}
