﻿using client.servers.clientServer;
using common;
using common.extends;
using Fleck;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.servers.clientServer
{


    public class ClientServer : IClientServer
    {
        private readonly ConcurrentDictionary<Guid, IWebSocketConnection> websockets = new();

        private readonly Dictionary<string, PluginPathCacheInfo> plugins = new();
        private readonly Dictionary<string, Tuple<object, MethodInfo>> pushPlugins = new();
        private readonly Dictionary<string, IClientServiceSettingPlugin> settingPlugins = new();


        private readonly Config config;
        private readonly ServiceProvider serviceProvider;

        public ClientServer(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;
            this.serviceProvider = serviceProvider;
            Notify();
        }

        public void LoadPlugins(Assembly[] assemblys)
        {
            Type voidType = typeof(void);
            Type asyncType = typeof(IAsyncResult);
            Type taskType = typeof(Task);

            IEnumerable<Type> types = assemblys
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IClientServicePlugin)));
            foreach (var item in types)
            {
                string path = item.Name.Replace("Plugin", "");
                object obj = serviceProvider.GetService(item);
                foreach (var method in item.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    string key = $"{path}/{method.Name}".ToLower();
                    if (!plugins.ContainsKey(key))
                    {
                        PluginPathCacheInfo cache = new PluginPathCacheInfo
                        {
                            IsVoid = method.ReturnType == voidType,
                            Method = method,
                            Target = obj,
                            IsTask = method.ReturnType.GetInterfaces().Contains(asyncType),
                            IsTaskResult = method.ReturnType.IsSubclassOf(taskType)
                        };
                        plugins.TryAdd(key, cache);
                    }
                }
            }

            IEnumerable<Type> types2 = assemblys
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IClientServerPushMsgPlugin)));
            foreach (var item in types2)
            {
                string path = item.Name.Replace("PushMsgPlugin", "");
                object obj = serviceProvider.GetService(item);
                foreach (var method in item.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    string key = $"{path}/{method.Name}".ToLower();
                    if (!pushPlugins.ContainsKey(key))
                    {
                        pushPlugins.TryAdd(key, new Tuple<object, MethodInfo>(obj, method));
                    }
                }
            }

            IEnumerable<Type> types3 = assemblys
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IClientServiceSettingPlugin)));
            foreach (var item in types3)
            {
                if (!settingPlugins.ContainsKey(item.Name))
                    settingPlugins.Add(item.Name, (IClientServiceSettingPlugin)serviceProvider.GetService(item));
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
                        dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { param });
                        object resultObject = null;
                        if (!plugin.IsVoid && resultAsync != null)
                        {
                            if (plugin.IsTask)
                            {
                                resultAsync.Wait();
                                if (plugin.IsTaskResult)
                                {
                                    resultObject = resultAsync.Result;
                                }
                            }
                            else
                            {
                                resultObject = resultAsync;
                            }
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
                            Content = pushPlugins.Select(c =>
                            {
                                return new ClientServiceMessageResponseWrap
                                {
                                    RequestId = 0,
                                    Code = 0,
                                    Content = c.Value.Item2.Invoke(c.Value.Item1, Array.Empty<object>()),
                                    Path = c.Key
                                };
                            })
                        }.ToJson());
                    }
                    System.Threading.Thread.Sleep(300);
                }
            });
        }

        public IClientServiceSettingPlugin GetSettingPlugin(string className)
        {
            settingPlugins.TryGetValue(className, out IClientServiceSettingPlugin plugin);
            return plugin;
        }

        public IEnumerable<SettingPluginInfo> GetSettingPlugins()
        {
            return settingPlugins.Select(c => new SettingPluginInfo
            {
                Name = c.Value.Name,
                Author = c.Value.Author,
                Desc = c.Value.Desc,
                ClassName = c.Value.GetType().Name
            });
        }

        public IEnumerable<string> GetPlugins()
        {
            return plugins.Select(c => c.Value.Target.GetType().Name).Distinct();
        }
    }

    public struct PluginPathCacheInfo
    {
        public object Target { get; set; }
        public MethodInfo Method { get; set; }
        public bool IsVoid { get; set; }
        public bool IsTask { get; set; }
        public bool IsTaskResult { get; set; }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddClientServer(this ServiceCollection obj)
        {
            obj.AddClientServer(AppDomain.CurrentDomain.GetAssemblies());
            obj.AddSingleton<IClientServer, ClientServer>();

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

            var types2 = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IClientServerPushMsgPlugin)));
            foreach (var item in types2)
            {
                obj.AddSingleton(item);
            }

            var types3 = assemblys.SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IClientServiceSettingPlugin)));
            foreach (var item in types3)
            {
                obj.AddSingleton(item);
            }

            return obj;
        }

        public static ServiceProvider UseClientServer(this ServiceProvider obj)
        {
            obj.UseClientServer(AppDomain.CurrentDomain.GetAssemblies());
            obj.GetService<IClientServer>().Start();
            return obj;
        }

        public static ServiceProvider UseClientServer(this ServiceProvider obj, Assembly[] assemblys)
        {
            obj.GetService<IClientServer>().LoadPlugins(assemblys);
            return obj;
        }
    }
}
