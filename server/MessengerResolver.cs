using common;
using common.extends;
using server;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace server
{
    public class MessengerResolver
    {
        private readonly Dictionary<string, PluginPathCacheInfo> plugins = new();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;
        private readonly MessengerSender messengerSender;


        public MessengerResolver(IUdpServer udpserver, ITcpServer tcpserver, MessengerSender messengerSender)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;
            this.messengerSender = messengerSender;

            this.tcpserver.OnPacket.SubAsync(async (wrap) =>
            {
                await InputData(wrap);
            });
            this.udpserver.OnPacket.SubAsync(async (wrap) =>
            {
                wrap.Connection.UpdateTime(messengerSender.LastTime);
                await InputData(wrap);
            });
        }
        public void LoadMessenger(Type type, object obj)
        {
            Type voidType = typeof(void);
            Type asyncType = typeof(IAsyncResult);
            Type taskType = typeof(Task);

            string path = type.Name.Replace("Messenger", "");
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
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

        public async Task InputData(ReceiveDataWrap serverDatawrap)
        {
            MessageTypes type = (MessageTypes)serverDatawrap.Data[serverDatawrap.Index];

            if (type == MessageTypes.RESPONSE)
            {
                var wrap = serverDatawrap.Connection.ReceiveResponseWrap;
                wrap.FromArray(serverDatawrap.Data, serverDatawrap.Index, serverDatawrap.Length);
                messengerSender.Response(wrap);
            }
            else
            {
                var wrap = serverDatawrap.Connection.ReceiveRequestWrap;
                serverDatawrap.Connection.ReceiveRequestWrap.FromArray(serverDatawrap.Data, serverDatawrap.Index, serverDatawrap.Length);
                try
                {
                    wrap.Path = wrap.Path.ToLower();
                    if (plugins.ContainsKey(wrap.Path))
                    {
                        PluginPathCacheInfo plugin = plugins[wrap.Path];
                        dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { serverDatawrap.Connection });
                        if (!plugin.IsVoid)
                        {
                            object resultObject = null;
                            if (plugin.IsTask)
                            {
                                await resultAsync;
                                if (plugin.IsTaskResult)
                                {
                                    resultObject = resultAsync.Result;
                                }
                            }
                            else
                            {
                                resultObject = resultAsync;
                            }
                            if (resultObject != null)
                            {
                                if (resultObject is byte[] byteData)
                                {
                                    await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                                    {
                                        Connection = serverDatawrap.Connection,
                                        Data = byteData,
                                        RequestId = wrap.RequestId
                                    });
                                }
                                else
                                {
                                    await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                                    {
                                        Connection = serverDatawrap.Connection,
                                        Data = resultObject.ToBytes(),
                                        RequestId = wrap.RequestId
                                    });
                                }
                            }
                            else
                            {
                                await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                                {
                                    Connection = serverDatawrap.Connection,
                                    Data = Array.Empty<byte>(),
                                    RequestId = wrap.RequestId
                                });
                            }
                        }
                    }
                    else
                    {
                        Logger.Instance.Error($"{wrap.Path} fot found");
                        await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                        {
                            Connection = serverDatawrap.Connection,
                            RequestId = wrap.RequestId,
                            Code = MessageResponeCodes.NOT_FOUND
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                    await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                    {
                        Connection = serverDatawrap.Connection,
                        RequestId = wrap.RequestId,
                        Code = MessageResponeCodes.ERROR
                    });
                }
            }
        }

        private struct PluginPathCacheInfo
        {
            public object Target { get; set; }
            public MethodInfo Method { get; set; }
            public bool IsVoid { get; set; }
            public bool IsTask { get; set; }
            public bool IsTaskResult { get; set; }
        }
    }
}