using common;
using common.extends;
using server.model;
using System;
using System.Collections.Generic;
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

            this.tcpserver.OnPacket.SubAsync(async (IConnection connection) =>
            {
                await InputData(connection);
            });
            this.udpserver.OnPacket.SubAsync(async (IConnection connection) =>
            {
                connection.UpdateTime(messengerSender.LastTime);
                await InputData(connection);
            });
        }
        public void LoadMessenger(Type type, object obj)
        {
            Type voidType = typeof(void);
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
                        IsTask = method.ReturnType.GetProperty("IsCompleted") != null && method.ReturnType.GetMethod("GetAwaiter") != null,
                        IsTaskResult = method.ReturnType.GetProperty("Result") != null
                    };
                    plugins.TryAdd(key, cache);
                }
            }
        }

        public async Task InputData(IConnection connection)
        {
            MessageTypes type = (MessageTypes)connection.ReceiveDataWrap.Data.Span[0];

            if (type == MessageTypes.RESPONSE)
            {
                var wrap = connection.ReceiveResponseWrap;
                wrap.FromArray(connection.ReceiveDataWrap.Data);
                messengerSender.Response(wrap);
            }
            else
            {
                var wrap = connection.ReceiveRequestWrap;
                wrap.FromArray(connection.ReceiveDataWrap.Data);
                try
                {
                    wrap.Path = wrap.Path.ToLower();
                    if (plugins.ContainsKey(wrap.Path))
                    {
                        PluginPathCacheInfo plugin = plugins[wrap.Path];
                        dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { connection });
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
                                        Connection = connection,
                                        Data = byteData,
                                        RequestId = wrap.RequestId
                                    });
                                }
                                else
                                {
                                    await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                                    {
                                        Connection = connection,
                                        Data = resultObject.ToBytes(),
                                        RequestId = wrap.RequestId
                                    });
                                }
                            }
                            else
                            {
                                await messengerSender.ReplyOnly(new MessageResponseParamsInfo
                                {
                                    Connection = connection,
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
                            Connection = connection,
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
                        Connection = connection,
                        RequestId = wrap.RequestId,
                        Code = MessageResponeCodes.ERROR
                    });
                }
                finally
                {
                    wrap.Memory = Memory<byte>.Empty;
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