using common;
using common.extends;
using server;
using server.extends;
using server.model;
using server.packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public class ServerPluginHelper
    {
        private readonly Dictionary<string, PluginPathCacheInfo> plugins = new();
        private NumberSpace requestIdNumberSpace = new NumberSpace(0);
        private ConcurrentDictionary<ulong, SendCacheModel> sends = new ConcurrentDictionary<ulong, SendCacheModel>();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;

        private long lastTime = Helper.GetTimeStamp();

        public ServerPluginHelper(IUdpServer udpserver, ITcpServer tcpserver)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;

            this.tcpserver.OnPacket.SubAsync(async (wrap) =>
            {
                for (int i = 0, len = wrap.Data.Length; i < len; i++)
                {
                    await InputData(wrap.Data[i], wrap);
                }
            });
            this.udpserver.OnPacket.SubAsync(async (wrap) =>
            {
                wrap.Connection.UpdateTime(lastTime);
                await InputData(wrap.Data, wrap);
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
                    lastTime = Helper.GetTimeStamp();
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

                MessageRequestWrap wrap = new MessageRequestWrap
                {
                    RequestId = msg.RequestId,
                    Content = msg.Data.ToBytes(),
                    Path = msg.Path
                };
                bool res = await msg.Connection.Send(wrap.ToArray());
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
        public async Task<bool> SendOnly(MessageRequestParamsWrap<byte[]> msg)
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
                MessageRequestWrap wrap = new MessageRequestWrap
                {
                    RequestId = msg.RequestId,
                    Content = msg.Data,
                    Path = msg.Path
                };

                bool res = await msg.Connection.Send(wrap.ToArray());
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

        public async Task InputData<T>(IPacket packet, ServerDataWrap<T> param)
        {
            MessageType type = (MessageType)packet.Chunk[0];

            if (type == MessageType.RESPONSE)
            {
                MessageResponseWrap wrap = new MessageResponseWrap();
                wrap.FromArray(packet.Chunk);
                if (sends.TryRemove(wrap.RequestId, out SendCacheModel send))
                {
                    send.Tcs.SetResult(new MessageRequestResponeWrap { Code = wrap.Code, Data = wrap.Memory });
                }
            }
            else
            {
                MessageRequestWrap wrap = new MessageRequestWrap();
                wrap.FromArray(packet.Chunk);
                try
                {
                    wrap.Path = wrap.Path.ToLower();
                    if (plugins.ContainsKey(wrap.Path))
                    {
                        PluginPathCacheInfo plugin = plugins[wrap.Path];
                        PluginParamWrap execute = new PluginParamWrap
                        {
                            Connection = param.Connection,
                            Packet = packet,
                            Wrap = wrap
                        };

                        dynamic resultAsync = plugin.Method.Invoke(plugin.Target, new object[] { execute });
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
                                if (resultObject is byte[])
                                {
                                    await SendReponseOnly(new MessageResponseParamsWrap
                                    {
                                        Connection = param.Connection,
                                        Data = resultObject as byte[],
                                        RequestId = wrap.RequestId
                                    });
                                }
                                else
                                {
                                    await SendReponseOnly(new MessageResponseParamsWrap
                                    {
                                        Connection = param.Connection,
                                        Data = resultObject.ToBytes(),
                                        RequestId = wrap.RequestId
                                    });
                                }
                            }
                            else
                            {
                                await SendReponseOnly(new MessageResponseParamsWrap
                                {
                                    Connection = param.Connection,
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
                            Connection = param.Connection,
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
                        Connection = param.Connection,
                        RequestId = wrap.RequestId,
                        Code = MessageResponeCode.ERROR
                    });
                }
            }
        }

        private async Task SendReponseOnly(MessageResponseParamsWrap msg)
        {
            try
            {
                MessageResponseWrap wrap = new MessageResponseWrap
                {
                    RequestId = msg.RequestId,
                    Content = msg.Data,
                    Code = msg.Code
                };

                await msg.Connection.Send(wrap.ToArray());
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
    public long Time { get; set; } = Helper.GetTimeStamp();
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