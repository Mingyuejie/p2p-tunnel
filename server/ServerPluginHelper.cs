using common;
using common.extends;
using server.extends;
using server.model;
using server.packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public class ServerPluginHelper
    {
        private readonly Dictionary<string, Tuple<object, MethodInfo>> plugins = new();
        private long requestId = 0;
        private ConcurrentDictionary<long, SendCacheModel> sends = new ConcurrentDictionary<long, SendCacheModel>();

        private readonly ITcpServer tcpserver;
        private readonly IUdpServer udpserver;

        private long sequence = 0;

        public ServerPluginHelper(IUdpServer udpserver, ITcpServer tcpserver)
        {
            this.tcpserver = tcpserver;
            this.udpserver = udpserver;

            this.tcpserver.OnPacket.Sub((wrap) =>
            {
                List<TcpPacket> bytesArray = TcpPacket.FromArray(wrap.Data);
                if (bytesArray.Count > 0)
                {
                    foreach (TcpPacket packet in bytesArray)
                    {
                        InputData(packet, wrap);
                    }
                }
            });
            this.udpserver.OnPacket.Sub((wrap) =>
            {
                UdpPacket packet = UdpPacket.FromArray(wrap.Address, wrap.Data);
                if (packet != null)
                {
                    InputData(packet, wrap);
                }
            });

            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (!sends.IsEmpty)
                    {
                        long time = Helper.GetTimeStamp();
                        foreach (SendCacheModel item in sends.Values)
                        {
                            if (time - item.Time > 15000)
                            {
                                if (sends.TryRemove(item.RequestId, out SendCacheModel cache) && cache != null)
                                {
                                    cache.Tcs?.SetResult(new ServerMessageResponeWrap
                                    {
                                        Code = ServerMessageResponeCodes.TIMEOUT,
                                        ErrorMsg = "请求超时"
                                    });
                                }
                            }
                        }
                    }
                    Thread.Sleep(1);
                }

            }, TaskCreationOptions.LongRunning);
        }

        public long NewRequestId()
        {
            Interlocked.Increment(ref requestId);
            return requestId;
        }
        public TaskCompletionSource<ServerMessageResponeWrap> NewReply(long requestId)
        {
            var tcs = new TaskCompletionSource<ServerMessageResponeWrap>();
            sends.TryAdd(requestId, new SendCacheModel { Tcs = tcs, RequestId = requestId });
            return tcs;
        }
        public void RemoveReply(long requestId)
        {
            sends.TryRemove(requestId, out _);
        }

        public async Task<ServerMessageResponeWrap> SendReplyTcp<T>(SendMessageWrap<T> msg)
        {
            if (msg.RequestId == 0)
            {
                msg.RequestId = NewRequestId();
            }
            TaskCompletionSource<ServerMessageResponeWrap> tcs = NewReply(msg.RequestId);
            if (!SendOnlyTcp(msg))
            {
                RemoveReply(msg.RequestId);
                tcs.SetResult(new ServerMessageResponeWrap { Code = ServerMessageResponeCodes.BAD_GATEWAY, ErrorMsg = "未运行" });
            }
            return await tcs.Task;
        }
        public bool SendOnlyTcp<T>(SendMessageWrap<T> msg)
        {
            if (msg.TcpCoket != null)
            {
                try
                {
                    if (msg.RequestId == 0)
                    {
                        msg.RequestId = NewRequestId();
                    }
                    ServerMessageWrap wrap = new ServerMessageWrap
                    {
                        RequestId = msg.RequestId,
                        Content = msg.Data.ToBytes(),
                        Path = msg.Path,
                        Type = msg.Type,
                        Code = msg.Code
                    };

                    TcpPacket tcpPackets = wrap.ToTcpPacket();
                    return tcpserver.Send(tcpPackets.ToArray(), msg.TcpCoket);
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex + "");
                }
            }
            return false;
        }

        public async Task<ServerMessageResponeWrap> SendReply<T>(SendMessageWrap<T> msg)
        {
            if (msg.RequestId == 0)
            {
                msg.RequestId = NewRequestId();
            }
            TaskCompletionSource<ServerMessageResponeWrap> tcs = NewReply(msg.RequestId);
            if (!SendOnly(msg))
            {
                RemoveReply(msg.RequestId);
                tcs.SetResult(new ServerMessageResponeWrap { Code = ServerMessageResponeCodes.BAD_GATEWAY, ErrorMsg = "未运行" });
            }
            return await tcs.Task;
        }
        public bool SendOnly<T>(SendMessageWrap<T> msg)
        {
            if (msg.Address != null)
            {
                try
                {
                    if (msg.RequestId == 0)
                    {
                        msg.RequestId = NewRequestId();
                    }
                    ServerMessageWrap wrap = new ServerMessageWrap
                    {
                        RequestId = msg.RequestId,
                        Content = msg.Data.ToBytes(),
                        Path = msg.Path,
                        Code = msg.Code,
                        Type = msg.Type
                    };

                    _ = Interlocked.Increment(ref sequence);
                    IEnumerable<UdpPacket> udpPackets = wrap.ToUdpPackets(sequence);

                    foreach (UdpPacket udpPacket in udpPackets)
                    {
                        byte[] udpPacketDatagram = udpPacket.ToArray();
                        udpserver.Send(udpPacketDatagram, msg.Address);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex + "");
                }
            }
            return false;
        }

        public void LoadPlugin(Type type, object obj)
        {
            string path = type.Name.Replace("Plugin", "");
            foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                string key = $"{path}/{method.Name}".ToLower();
                if (!plugins.ContainsKey(key))
                {
                    plugins.TryAdd(key, new Tuple<object, MethodInfo>(obj, method));
                }
            }
        }

        private void ReplayData<T>(SendMessageWrap<T> data, ServerType serverType)
        {
            if (serverType == ServerType.TCP)
            {
                SendOnlyTcp(data);
            }
            else
            {
                SendOnly(data);
            }
        }

        private void InputData<T>(IPacket packet, ServerDataWrap<T> param)
        {
            ServerMessageWrap wrap = packet.Chunk.DeBytes<ServerMessageWrap>();
            if (wrap.Type == ServerMessageTypes.RESPONSE)
            {
                if (sends.TryRemove(wrap.RequestId, out SendCacheModel send) && send != null)
                {
                    send.Tcs.SetResult(new ServerMessageResponeWrap { Code = wrap.Code, ErrorMsg = Encoding.UTF8.GetString(wrap.Content), Data = wrap.Content });
                }
            }
            else
            {
                try
                {
                    wrap.Path = wrap.Path.ToLower();
                    if (plugins.ContainsKey(wrap.Path))
                    {
                        var plugin = plugins[wrap.Path];
                        PluginParamWrap excute = new PluginParamWrap
                        {
                            TcpSocket = param.Socket,
                            Packet = packet,
                            ServerType = param.ServerType,
                            SourcePoint = param.Address,
                            Wrap = wrap
                        };

                        dynamic resultAsync = plugin.Item2.Invoke(plugin.Item1, new object[] { excute });
                        if (excute.Code == ServerMessageResponeCodes.OK)
                        {
                            if (resultAsync != null)
                            {
                                object resultObject = null;
                                if (resultAsync is Task task)
                                {
                                    resultAsync.Wait();
                                    resultObject = resultAsync.Result;
                                }
                                else
                                {
                                    resultObject = resultAsync;
                                }
                                if (resultObject != null)
                                {
                                    ReplayData(new SendMessageWrap<object>
                                    {
                                        TcpCoket = param.Socket,
                                        Address = param.Address,
                                        Code = excute.Code,
                                        Data = resultObject,
                                        RequestId = wrap.RequestId,
                                        Path = wrap.Path,
                                        Type = ServerMessageTypes.RESPONSE
                                    }, param.ServerType);
                                }
                            }
                        }
                        else
                        {
                            ReplayData(new SendMessageWrap<object>
                            {
                                TcpCoket = param.Socket,
                                Address = param.Address,
                                Code = excute.Code,
                                Data = excute.ErrorMessage,
                                RequestId = wrap.RequestId,
                                Path = wrap.Path,
                                Type = ServerMessageTypes.RESPONSE
                            }, param.ServerType);
                        }
                    }
                    else
                    {
                        ReplayData(new SendMessageWrap<object>
                        {
                            TcpCoket = param.Socket,
                            Address = param.Address,
                            Code = ServerMessageResponeCodes.BAD_GATEWAY,
                            Data = "没找到对应的插件执行你的操作",
                            RequestId = wrap.RequestId,
                            Path = wrap.Path,
                            Type = ServerMessageTypes.RESPONSE
                        }, param.ServerType);
                    }
                }
                catch (Exception ex)
                {
                    ReplayData(new SendMessageWrap<object>
                    {
                        TcpCoket = param.Socket,
                        Address = param.Address,
                        Code = ServerMessageResponeCodes.BAD_GATEWAY,
                        Data = ex.Message,
                        RequestId = wrap.RequestId,
                        Path = wrap.Path,
                        Type = ServerMessageTypes.RESPONSE
                    }, param.ServerType);
                }
            }
        }
    }
}

public class SendCacheModel
{
    public TaskCompletionSource<ServerMessageResponeWrap> Tcs { get; set; }
    public long Time { get; set; } = Helper.GetTimeStamp();
    public long RequestId { get; set; } = 0;
}
