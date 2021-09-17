using common;
using common.extends;
using server.extends;
using server.model;
using server.models;
using server.packet;
using server.plugin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public class TCPServer : ITcpServer
    {
        private long Id = 0;

        private ConcurrentDictionary<int, ServerModel> servers = new ConcurrentDictionary<int, ServerModel>();

        private CancellationTokenSource cancellationTokenSource;
        private bool Running
        {
            get
            {
                return cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested;
            }
        }

        public TCPServer()
        {
            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (!Plugin.sends.IsEmpty)
                    {
                        long time = Helper.GetTimeStamp();
                        foreach (SendCacheModel item in Plugin.sends.Values)
                        {
                            if (time - item.Time > 15000)
                            {
                                if (Plugin.sends.TryRemove(item.RequestId, out SendCacheModel cache) && cache != null)
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

        public void Start(int port, IPAddress ip = null)
        {
            if (Running)
            {
                return;
            }
            cancellationTokenSource = new CancellationTokenSource();
            BindAccept(port, ip ?? IPAddress.Any, cancellationTokenSource);

        }
        public void BindAccept(int port, IPAddress ip, CancellationTokenSource tokenSource)
        {
            if (servers.ContainsKey(port)) return;

            var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(ip, port));
            socket.Listen(int.MaxValue);

            ServerModel server = new()
            {
                AcceptDone = new ManualResetEvent(false),
                Socket = socket
            };
            servers.TryAdd(port, server);

            _ = Task.Factory.StartNew((e) =>
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    server.AcceptDone.Reset();
                    try
                    {
                        _ = socket.BeginAccept(new AsyncCallback(Accept), server);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Debug(ex + "");
                        Stop();
                        break;
                    }
                    _ = server.AcceptDone.WaitOne();
                }

            }, TaskCreationOptions.LongRunning, tokenSource.Token);
        }
        public void Stop()
        {
            cancellationTokenSource.Cancel();
            ReceiveModel.ClearAll();
            foreach (ServerModel server in servers.Values)
            {

                if (server != null && server.Socket != null)
                {
                    server.AcceptDone.Dispose();
                    server.Socket.SafeClose();
                }
            }
            servers.Clear();
        }
        public async Task<ServerMessageResponeWrap> SendReply<T>(SendMessageWrap<T> msg)
        {
            var tcs = new TaskCompletionSource<ServerMessageResponeWrap>();
            if (Running && msg.TcpCoket != null && msg.TcpCoket.Connected)
            {
                try
                {
                    if (msg.RequestId == 0)
                    {
                        Interlocked.Increment(ref Plugin.requestId);
                        msg.RequestId = Plugin.requestId;
                    }

                    Plugin.sends.TryAdd(msg.RequestId, new SendCacheModel { Tcs = tcs, RequestId = msg.RequestId });

                    ServerMessageWrap wrap = new ServerMessageWrap
                    {
                        RequestId = msg.RequestId,
                        Content = msg.Data.ToBytes(),
                        Path = msg.Path,
                        Type = msg.Type,
                        Code = msg.Code
                    };

                    TcpPacket tcpPackets = wrap.ToTcpPacket();
                    msg.TcpCoket.SendTimeout = msg.Timeout;
                    _ = msg.TcpCoket.Send(tcpPackets.ToArray());
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex + "");
                }
            }
            else
            {
                tcs.SetResult(new ServerMessageResponeWrap { Code = ServerMessageResponeCodes.BAD_GATEWAY, ErrorMsg = "未运行" });
            }
            return await tcs.Task;
        }

        public void SendOnly<T>(SendMessageWrap<T> msg)
        {
            if (Running && msg.TcpCoket != null && msg.TcpCoket.Connected)
            {
                try
                {

                    if (msg.RequestId == 0)
                    {
                        Interlocked.Increment(ref Plugin.requestId);
                        msg.RequestId = Plugin.requestId;
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
                    msg.TcpCoket.SendTimeout = msg.Timeout;
                    _ = msg.TcpCoket.Send(tcpPackets.ToArray());
                }
                catch (Exception ex)
                {
                    Logger.Instance.Debug(ex + "");
                }
            }
        }

        private void Accept(IAsyncResult result)
        {
            ServerModel server = (ServerModel)result.AsyncState;
            _ = server.AcceptDone.Set();

            try
            {
                Socket client = server.Socket.EndAccept(result);
                BindReceive(client);
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex + "");
            }
        }
        private void Receive(IAsyncResult result)
        {
            ReceiveModel model = (ReceiveModel)result.AsyncState;
            try
            {
                if (model.Socket != null && model.Socket.Connected)
                {
                    int length = model.Socket.EndReceive(result);
                    result.AsyncWaitHandle.Close();
                    if (length > 0)
                    {
                        if (Running)
                        {
                            if (length == model.Buffer.Length)
                            {
                                Receive(model, model.Buffer);
                            }
                            else
                            {
                                byte[] bytes = new byte[length];
                                Array.Copy(model.Buffer, 0, bytes, 0, bytes.Length);
                                Receive(model, bytes);
                            }
                            _ = model.Socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
                        }
                        else
                        {
                            Logger.Instance.Debug("!Running");
                        }
                    }
                    else
                    {
                        Logger.Instance.Debug($"length:{length}");
                        model.Clear();
                    }
                }

            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionAborted)
                {
                }
                model.ErrorCallback?.Invoke(ex.SocketErrorCode);
                Logger.Instance.Debug(ex + "");
                model.Clear();
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex + "");
                model.Clear();
            }
        }
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null)
        {
            IPEndPoint ip = IPEndPoint.Parse(socket.RemoteEndPoint.ToString());
            Interlocked.Increment(ref Id);
            ReceiveModel model = new ReceiveModel { ErrorCallback = errorCallback, Id = Id, Address = ip, Socket = socket, Buffer = new byte[0] };
            _ = ReceiveModel.Add(model);

            model.Buffer = new byte[1024];
            _ = socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
        }
        private void Receive(ReceiveModel model, byte[] buffer)
        {
            IPEndPoint address = IPEndPoint.Parse(model.Socket.RemoteEndPoint.ToString());
            lock (model.CacheBuffer)
            {
                model.CacheBuffer.AddRange(buffer);
            }
            List<TcpPacket> bytesArray = TcpPacket.FromArray(model.CacheBuffer);
            if (bytesArray.Count > 0)
            {
                foreach (TcpPacket packet in bytesArray)
                {
                    ServerMessageWrap wrap = packet.Chunk.DeBytes<ServerMessageWrap>();
                    if (wrap.Type == ServerMessageTypes.RESPONSE)
                    {
                        if (Plugin.sends.TryRemove(wrap.RequestId, out SendCacheModel send) && send != null)
                        {
                            Logger.Instance.Debug($"TCP {wrap.Path} 花费时间 {Helper.GetTimeStamp()- send.Time} ms");
                            send.Tcs.SetResult(new ServerMessageResponeWrap { Code = wrap.Code, ErrorMsg = wrap.Code.ToString(), Data = wrap.Content });
                        }
                    }
                    else
                    {
                        try
                        {
                            wrap.Path = wrap.Path.ToLower();
                            if (Plugin.plugins.ContainsKey(wrap.Path))
                            {
                                var plugin = Plugin.plugins[wrap.Path];
                                PluginParamWrap excute = new PluginParamWrap
                                {
                                    TcpSocket = model.Socket,
                                    Packet = packet,
                                    ServerType = ServerType.TCP,
                                    SourcePoint = address,
                                    Wrap = wrap
                                };

                                object resultAsync = plugin.Item2.Invoke(plugin.Item1, new object[] { excute });
                                if (excute.Code == ServerMessageResponeCodes.OK)
                                {
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
                                    if (resultObject != null)
                                    {
                                        SendOnly(new SendMessageWrap<object>
                                        {
                                            TcpCoket = model.Socket,
                                            Code = excute.Code,
                                            Data = resultObject,
                                            RequestId = wrap.RequestId,
                                            Path = wrap.Path,
                                            Type = ServerMessageTypes.RESPONSE
                                        });
                                    }
                                }
                                else
                                {
                                    SendOnly(new SendMessageWrap<object>
                                    {
                                        TcpCoket = model.Socket,
                                        Code = excute.Code,
                                        Data = excute.ErrorMessage,
                                        RequestId = wrap.RequestId,
                                        Path = wrap.Path,
                                        Type = ServerMessageTypes.RESPONSE
                                    });
                                }
                            }
                            else
                            {
                                SendOnly(new SendMessageWrap<string>
                                {
                                    TcpCoket = model.Socket,
                                    Code = ServerMessageResponeCodes.BAD_GATEWAY,
                                    Data = "没找到对应的插件执行你的操作",
                                    RequestId = wrap.RequestId,
                                    Path = wrap.Path,
                                    Type = ServerMessageTypes.RESPONSE
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            SendOnly(new SendMessageWrap<string>
                            {
                                TcpCoket = model.Socket,
                                Code = ServerMessageResponeCodes.BAD_GATEWAY,
                                Data = ex.Message,
                                RequestId = wrap.RequestId,
                                Path = wrap.Path,
                                Type = ServerMessageTypes.RESPONSE
                            });
                        }
                    }
                }
            }
        }
    }

    public class ReceiveModel
    {
        public static ConcurrentDictionary<long, ReceiveModel> clients = new ConcurrentDictionary<long, ReceiveModel>();

        public long Id { get; set; }
        public long ConnectId { get; set; }
        public Socket Socket { get; set; }
        public IPEndPoint Address { get; set; }
        public byte[] Buffer { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();

        public Action<SocketError> ErrorCallback { get; set; }

        public void Clear()
        {
            clients.TryRemove(Id, out _);

            Id = 0;
            ConnectId = 0;
            Address = null;
            CacheBuffer.Clear();
            Buffer = null;
            ErrorCallback = null;

            if (Socket != null)
            {
                Socket.SafeClose();
                Socket = null;
            }

        }

        public static void ClearAll()
        {
            foreach (ReceiveModel client in clients.Values)
            {
                if (client != null)
                {
                    client.Clear();
                }
            }
        }

        public static bool Add(ReceiveModel model)
        {
            return clients.TryAdd(model.Id, model);
        }
    }

    public class ServerModel
    {
        public Socket Socket { get; set; }
        public ManualResetEvent AcceptDone { get; set; }
    }

}
