using client.service.clientService.plugins;
using client.service.p2pPlugins.plugins.fileServer;
using client.service.p2pPlugins.plugins.forward.tcp;
using common;
using common.extends;
using Fleck;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
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
                                  Logger.Instance.Info(ex + "");
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
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = AppShareData.Instance.Clients.Values.ToList().ToJson(),
                            RequestId = 0,
                            Path = "clients/list",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = new RegisterInfo
                            {
                                ClientConfig = AppShareData.Instance.ClientConfig,
                                ServerConfig = AppShareData.Instance.ServerConfig,
                                LocalInfo = AppShareData.Instance.LocalInfo,
                                RemoteInfo = AppShareData.Instance.RemoteInfo,
                            }.ToJson(),
                            RequestId = 0,
                            Path = "register/info",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = TcpForwardHelper.Instance.Mappings.ToJson(),
                            RequestId = 0,
                            Path = "tcpforward/list",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = AppShareData.Instance.FileServerConfig.ToJson(),
                            RequestId = 0,
                            Path = "fileserver/info",
                            Code = 0
                        }.ToJson());
                        connection.Send(new ClientServiceMessageWrap
                        {
                            Content = FileServerHelper.Instance.GetOnlineList().ToJson(),
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
}
