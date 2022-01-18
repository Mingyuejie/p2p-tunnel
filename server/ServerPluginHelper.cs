using common;
using common.extends;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace server
{
    public class ServerPluginHelper
    {
        private readonly Dictionary<string, PluginPathCacheInfo> plugins = new();
        private NumberSpace requestIdNumberSpace = new NumberSpace(0);
        private ConcurrentDictionary<ulong, SendCacheModel> sends = new ConcurrentDictionary<ulong, SendCacheModel>();
        private SimpleObjectPool<MessageRequestWrap> messageRequestWrapPool = new SimpleObjectPool<MessageRequestWrap>();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;

        private long lastTime = DateTimeHelper.GetTimeStamp();

        public ServerPluginHelper(IUdpServer udpserver, ITcpServer tcpserver)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;

            this.tcpserver.OnPacket.SubAsync(async (wrap) =>
            {
                await InputData(wrap);
            });
            this.udpserver.OnPacket.SubAsync(async (wrap) =>
            {
                wrap.Connection.UpdateTime(lastTime);
                await InputData(wrap);
            });

            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    if (!sends.IsEmpty)
                    {
                        foreach (SendCacheModel item in sends.Values)
                        {
                            if (item.Timeout > 0 && lastTime - item.Time > item.Timeout)
                            {
                                if (sends.TryRemove(item.RequestId, out SendCacheModel cache))
                                {
                                    cache.Tcs.SetResult(new MessageRequestResponeWrap
                                    {
                                        Code = MessageResponeCode.TIMEOUT
                                    });
                                }
                            }
                        }
                    }
                    lastTime = DateTimeHelper.GetTimeStamp();
                    await Task.Delay(1);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public void LoadPlugin(Type type, object obj)
        {
            Type voidType = typeof(void);
            Type asyncType = typeof(IAsyncResult);
            Type taskType = typeof(Task);

            string path = type.Name.Replace("Plugin", "");
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

        public async Task<MessageRequestResponeWrap> SendReply<T>(MessageRequestParamsWrap<T> msg)
        {
            if (msg.RequestId == 0)
            {
                msg.RequestId = requestIdNumberSpace.Get();
            }
            TaskCompletionSource<MessageRequestResponeWrap> tcs = NewReply(msg.RequestId, msg.Timeout);
            if (!await SendOnly(msg))
            {
                sends.TryRemove(msg.RequestId, out _);
                tcs.SetResult(new MessageRequestResponeWrap { Code = MessageResponeCode.NOT_CONNECT });
            }
            return await tcs.Task;
        }
        public async Task<MessageRequestResponeWrap> SendReply(MessageRequestParamsWrap<byte[]> msg)
        {
            if (msg.RequestId == 0)
            {
                msg.RequestId = requestIdNumberSpace.Get();
            }
            TaskCompletionSource<MessageRequestResponeWrap> tcs = NewReply(msg.RequestId, msg.Timeout);
            if (!await SendOnly(msg))
            {
                sends.TryRemove(msg.RequestId, out _);
                tcs.SetResult(new MessageRequestResponeWrap { Code = MessageResponeCode.NOT_CONNECT });
            }
            return await tcs.Task;
        }
        public async Task<bool> SendOnly<T>(MessageRequestParamsWrap<T> msg)
        {
            try
            {
                if (msg.RequestId == 0)
                {
                    msg.RequestId = requestIdNumberSpace.Get();
                }
                if (msg.Connection == null)
                {
                    return false;
                }
                MessageRequestWrap wrap = messageRequestWrapPool.Get();
                wrap.Content = msg.Data.ToBytes();
                wrap.RequestId = msg.RequestId;
                wrap.Path = msg.Path;

                var sendData = wrap.ToArray(msg.Connection.ServerType);
                bool res = await msg.Connection.Send(sendData);
                messageRequestWrapPool.Restore(wrap);

                if (res && msg.Connection.ServerType == ServerType.UDP)
                {
                    msg.Connection.UpdateTime(lastTime);
                }
                return res;
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex);
            }
            return false;
        }

        public async Task InputData(ServerDataWrap serverDatawrap)
        {
            MessageType type = (MessageType)serverDatawrap.Data[serverDatawrap.Index];

            if (type == MessageType.RESPONSE)
            {
                var wrap = serverDatawrap.Connection.ReceiveResponseWrap;
                wrap.FromArray(serverDatawrap.Data, serverDatawrap.Index, serverDatawrap.Length);
                if (sends.TryRemove(serverDatawrap.Connection.ReceiveResponseWrap.RequestId, out SendCacheModel send))
                {
                    send.Tcs.SetResult(new MessageRequestResponeWrap { Code = wrap.Code, Data = wrap.Memory });
                }
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
                                await (resultAsync as Task);
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
                                    await SendReponseOnly(new MessageResponseParamsWrap
                                    {
                                        Connection = serverDatawrap.Connection,
                                        Data = byteData,
                                        RequestId = wrap.RequestId
                                    });
                                }
                                else
                                {
                                    await SendReponseOnly(new MessageResponseParamsWrap
                                    {
                                        Connection = serverDatawrap.Connection,
                                        Data = resultObject.ToBytes(),
                                        RequestId = wrap.RequestId
                                    });
                                }
                            }
                            else
                            {
                                await SendReponseOnly(new MessageResponseParamsWrap
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
                        await SendReponseOnly(new MessageResponseParamsWrap
                        {
                            Connection = serverDatawrap.Connection,
                            RequestId = wrap.RequestId,
                            Code = MessageResponeCode.NOT_FOUND
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                    await SendReponseOnly(new MessageResponseParamsWrap
                    {
                        Connection = serverDatawrap.Connection,
                        RequestId = wrap.RequestId,
                        Code = MessageResponeCode.ERROR
                    });
                }
            }
        }

        private TaskCompletionSource<MessageRequestResponeWrap> NewReply(ulong requestId, int timeout = 15000)
        {
            TaskCompletionSource<MessageRequestResponeWrap> tcs = new TaskCompletionSource<MessageRequestResponeWrap>();
            if (timeout == 0)
            {
                timeout = 15000;
            }
            sends.TryAdd(requestId, new SendCacheModel { Tcs = tcs, RequestId = requestId, Timeout = timeout });
            return tcs;
        }
        private async Task SendReponseOnly(MessageResponseParamsWrap msg)
        {
            try
            {
                MessageResponseWrap wrap = new MessageResponseWrap
                {
                    Content = msg.Data.ToBytes(),
                    RequestId = msg.RequestId,
                    Code = msg.Code,
                };

                var sendData = wrap.ToArray(msg.Connection.ServerType);
                bool res = await msg.Connection.Send(sendData);
                if (res && msg.Connection.ServerType == ServerType.UDP)
                {
                    msg.Connection.UpdateTime(lastTime);
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex);
            }
        }
    }
}

public class MessageRequestResponeWrap
{
    public MessageResponeCode Code { get; set; } = MessageResponeCode.OK;
    public ReadOnlyMemory<byte> Data { get; set; } = Array.Empty<byte>();
}

public class SendCacheModel
{
    public TaskCompletionSource<MessageRequestResponeWrap> Tcs { get; set; }
    public long Time { get; set; } = DateTimeHelper.GetTimeStamp();
    public ulong RequestId { get; set; } = 0;
    public int Timeout { get; set; } = 15000;
}

public struct PluginPathCacheInfo
{
    public object Target { get; set; }
    public MethodInfo Method { get; set; }
    public bool IsVoid { get; set; }
    public bool IsTask { get; set; }
    public bool IsTaskResult { get; set; }
}