using common;
using common.extends;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using server.plugin;
using server.service.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace server.service.plugins
{
    /// <summary>
    /// TCP代理转发
    /// </summary>
    public class TcpForwardPlugin : IPlugin
    {
        private readonly TcpForwardServer tcpForwardServer;
        private readonly Config config;
        private readonly IClientRegisterCaching clientRegisterCache;

        public TcpForwardPlugin(TcpForwardServer tcpForwardServer, Config config, IClientRegisterCaching clientRegisterCache)
        {
            this.tcpForwardServer = tcpForwardServer;
            this.config = config;
            this.clientRegisterCache = clientRegisterCache;
        }

        public void Register(PluginParamWrap data)
        {
            if (!config.tcpForward)
            {
                data.SetCode(ServerMessageResponeCodes.ACCESS, "服务端未开启TCP转发");
                return;
            }
            TcpForwardRegisterModel foreards = data.Wrap.Content.DeBytes<TcpForwardRegisterModel>();
            if (!clientRegisterCache.Verify(foreards.Id, data))
            {
                data.SetCode(ServerMessageResponeCodes.ACCESS, "认证失败");
                return;
            }
            RegisterCacheModel source = clientRegisterCache.Get(foreards.Id);
            if (source == null)
            {
                data.SetCode(ServerMessageResponeCodes.ACCESS, "未注册");
                return;
            }

            tcpForwardServer.StopAll();
            foreach (var webs in foreards.Web)
            {
                tcpForwardServer.Start(new TcpForwardRecordModel
                {
                    AliveType = TcpForwardAliveTypes.WEB,
                    Client = source,
                    ServerPort = webs.Port,
                    TargetIp = string.Empty,
                    TargetPort = 0,
                    WebForwards = webs.Forwards
                });
            }
            foreach (var tunnels in foreards.Tunnel)
            {
                tcpForwardServer.Start(new TcpForwardRecordModel
                {
                    AliveType = TcpForwardAliveTypes.TUNNEL,
                    Client = source,
                    ServerPort = tunnels.Port,
                    TargetIp = tunnels.TargetIp,
                    TargetPort = tunnels.TargetPort,

                });
            }
        }
        public void Response(PluginParamWrap data)
        {
            tcpForwardServer.Response(data.Wrap.Content.DeBytes<TcpForwardModel>());
        }
    }


    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection obj)
        {
            obj.AddSingleton<TcpForwardServer>();

            return obj;
        }
        public static ServiceProvider UseTcpForwardPlugin(this ServiceProvider obj)
        {
            return obj;
        }
    }

    public class TcpForwardServer
    {
        private readonly ServerPluginHelper serverPluginHelper;
        public TcpForwardServer(ServerPluginHelper serverPluginHelper)
        {
            this.serverPluginHelper = serverPluginHelper;
        }

        private long requestId = 0;

        public void Start(TcpForwardRecordModel mapping)
        {
            if (ServerModel.Contains(mapping.ServerPort))
            {
                return;
            }

            Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.Bind(new IPEndPoint(IPAddress.Any, mapping.ServerPort));
            socket.Listen(int.MaxValue);


            ServerModel server = new ServerModel
            {
                CancelToken = new CancellationTokenSource(),
                AcceptDone = new ManualResetEvent(false),
                Socket = socket,
                Port = mapping.ServerPort
            };
            ServerModel.Add(server);

            int sourcePort = mapping.ServerPort;
            int targetPort = mapping.TargetPort;
            string targetIp = mapping.TargetIp;
            RegisterCacheModel targetClient = mapping.Client;
            TcpForwardAliveTypes aliveType = mapping.AliveType;

            _ = Task.Factory.StartNew((e) =>
            {
                while (!server.CancelToken.IsCancellationRequested)
                {
                    _ = server.AcceptDone.Reset();
                    try
                    {
                        _ = socket.BeginAccept(new AsyncCallback(Accept), new ClientModel2
                        {
                            TargetClient = targetClient,
                            TargetPort = targetPort,
                            AliveType = aliveType,
                            TargetIp = targetIp,
                            SourceSocket = socket,
                            SourcePort = sourcePort,
                            AcceptDone = server.AcceptDone,
                            WebForwards = mapping.WebForwards
                        });
                    }
                    catch (Exception)
                    {
                        Stop(sourcePort);
                        server.Remove();
                        break;
                    }
                    _ = server.AcceptDone.WaitOne();
                }
            }, TaskCreationOptions.LongRunning, server.CancelToken.Token);

        }
        private void Accept(IAsyncResult result)
        {
            ClientModel2 server = (ClientModel2)result.AsyncState;
            server.AcceptDone.Set();
            try
            {
                Socket socket = server.SourceSocket.EndAccept(result);
                _ = Interlocked.Increment(ref requestId);

                ClientCacheModel client = new ClientCacheModel
                {
                    RequestId = requestId,
                    SourcePort = server.SourcePort,
                    Socket = socket
                };
                ClientCacheModel.Add(client);
                client.Stream = new NetworkStream(socket, false);
                ClientModel2 client1 = new()
                {
                    RequestId = client.RequestId,
                    TargetPort = server.TargetPort,
                    AliveType = server.AliveType,
                    TargetIp = server.TargetIp,
                    SourceSocket = socket,
                    TargetClient = server.TargetClient,
                    Stream = client.Stream,
                    WebForwards = server.WebForwards
                };
                BindReceive(client1);
            }
            catch (Exception)
            {
                Stop(server.SourcePort);
            }
        }

        public void BindReceive(ClientModel2 client)
        {
            Task.Run(() =>
            {
                while (client.Stream.CanRead && ClientCacheModel.Contains(client.RequestId))
                {
                    try
                    {
                        var bytes = client.Stream.ReceiveAll();
                        if (bytes.Length > 0)
                        {
                            Receive(client, bytes);
                        }
                        else
                        {
                            ClientCacheModel.Remove(client.RequestId);
                        }

                    }
                    catch (Exception)
                    {
                        ClientCacheModel.Remove(client.RequestId);
                        break;
                    }
                }
                ClientCacheModel.Remove(client.RequestId);
            });
        }
        private void Receive(ClientModel2 client, byte[] data)
        {
            string targetIp = client.TargetIp;
            int targetPort = client.TargetPort;
            //web的时候，根据请求的域名 指向不同的目标ip和端口，所以需要解析一下 request headers 拿到host
            if (client.AliveType == TcpForwardAliveTypes.WEB)
            {
                var watch = new MyStopwatch();
                watch.Start();
                string host = GetHost(data);
                watch.Stop();
                watch.Output("解析host耗时:");
                if (client.WebForwards.ContainsKey(host))
                {
                    var item = client.WebForwards[host];
                    targetPort = item.TargetPort;
                    targetIp = item.TargetIp;
                }
            }

            SendMessageWrap<TcpForwardModel> model = new SendMessageWrap<TcpForwardModel>
            {
                Code = ServerMessageResponeCodes.OK,
                Path = "servertcpforward/excute",
                RequestId = client.RequestId,
                TcpCoket = client.SourceSocket,
                Type = ServerMessageTypes.REQUEST,
                Data = new TcpForwardModel
                {
                    RequestId = client.RequestId,
                    Buffer = data,
                    Type = TcpForwardType.REQUEST,
                    TargetPort = targetPort,
                    AliveType = client.AliveType,
                    TargetIp = targetIp
                },
            }; ;
            if (client.SourceSocket == null || !client.SourceSocket.Connected)
            {
                Fail(model.Data, "未选择转发对象，或者未与转发对象建立连接");
                return;
            }
            serverPluginHelper.SendOnlyTcp(model);
        }

        public void Response(TcpForwardModel model)
        {
            if (ClientCacheModel.Get(model.RequestId, out ClientCacheModel client) && client != null)
            {
                try
                {
                    if (client.Stream.CanWrite)
                    {
                        client.Stream.Write(model.Buffer);
                        client.Stream.Flush();
                    }
                }
                catch (Exception)
                {
                }
            }
        }
        public void Fail(TcpForwardModel failModel, string body = "")
        {
            if (ClientCacheModel.Get(failModel.RequestId, out ClientCacheModel client) && client != null)
            {
                if (failModel.AliveType == TcpForwardAliveTypes.WEB)
                {
                    StringBuilder sb = new StringBuilder();
                    if (failModel.Buffer.IsOptionsMethod())
                    {
                        sb.Append("HTTP/1.1 204 No Content\r\n");
                    }
                    else
                    {
                        sb.Append("HTTP/1.1 404 Not Found\r\n");
                    }
                    sb.Append("Content-Type: text/html;charset=utf-8\r\n");
                    sb.Append("Access-Control-Allow-Credentials: true\r\n");
                    sb.Append("Access-Control-Allow-Headers: *\r\n");
                    sb.Append("Access-Control-Allow-Methods: *\r\n");
                    sb.Append("Access-Control-Allow-Origin: *\r\n");
                    sb.Append("\r\n");
                    client.Stream.Write(Encoding.UTF8.GetBytes(sb.ToString()));

                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        client.Stream.Write(Encoding.UTF8.GetBytes(body));
                    }
                    else if (failModel.Buffer != null && failModel.Buffer.Length > 0)
                    {
                        client.Stream.Write(failModel.Buffer);
                    }
                    client.Stream.Flush();
                }
                else
                {
                    Logger.Instance.Error(Encoding.UTF8.GetString(failModel.Buffer));
                }
                client.Remove();
            }
        }
        public void Stop(ServerModel model)
        {
            ClientCacheModel.Clear(model.Port);
            model.Remove();
        }
        public void Stop(int sourcePort)
        {
            if (ServerModel.Get(sourcePort, out ServerModel model))
            {
                Stop(model);
            }
        }
        public void StopAll()
        {
            foreach (ServerModel item in ServerModel.All())
            {
                Stop(item);
            }
        }


        private byte[] hostBytes = Encoding.ASCII.GetBytes("Host:");
        private string GetHost(byte[] bytes)
        {
            int lastIndex = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 10 && bytes[i - 1] == 13)
                {
                    if (bytes.Length - i >= hostBytes.Length)
                    {
                        if (Enumerable.SequenceEqual(bytes.Skip(lastIndex).Take(hostBytes.Length), hostBytes))
                        {
                            return Encoding.UTF8.GetString(bytes.Skip(lastIndex + hostBytes.Length + 1).Take(i - lastIndex - hostBytes.Length - 2).ToArray()).Split(':')[0];
                        }
                    }
                    lastIndex = i + 1;
                }
                //头结束
                if (bytes[i] == 10 && bytes[i - 1] == 13 && bytes[i - 2] == 10 && bytes[i - 3] == 13)
                {
                    break;
                }
            }
            return string.Empty;
        }
    }

    [ProtoContract, MessagePackObject]
    public class TcpForwardRegisterModel
    {
        [ProtoMember(1), Key(1)]
        public ForwardWebModel[] Web { get; set; } = Array.Empty<ForwardWebModel>();
        [ProtoMember(2), Key(2)]
        public ForwardTunnelModel[] Tunnel { get; set; } = Array.Empty<ForwardTunnelModel>();

        [ProtoMember(3), Key(3), JsonIgnore]
        public long Id { get; set; } = 0;
    }
    [ProtoContract, MessagePackObject]
    public class ForwardWebModel
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 0;
        [ProtoMember(2), Key(2)]
        public Dictionary<string, ForwardWebItem> Forwards { get; set; } = new Dictionary<string, ForwardWebItem>();
    }
    [ProtoContract, MessagePackObject]
    public class ForwardWebItem
    {
        [ProtoMember(2), Key(2)]
        public int TargetPort { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }
    [ProtoContract, MessagePackObject]
    public class ForwardTunnelModel
    {
        [ProtoMember(1), Key(1)]
        public int Port { get; set; } = 0;
        [ProtoMember(2), Key(2)]
        public int TargetPort { get; set; } = 0;
        [ProtoMember(3), Key(3)]
        public string TargetIp { get; set; } = string.Empty;
    }

    public class ClientModel2 : ClientModel
    {
        public RegisterCacheModel TargetClient { get; set; }
        public ManualResetEvent AcceptDone { get; set; }

        public Dictionary<string, ForwardWebItem> WebForwards { get; set; } = new Dictionary<string, ForwardWebItem>();
    }
    public class ClientCacheModel
    {
        private static ConcurrentDictionary<long, ClientCacheModel> clients = new();

        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
        public long RequestId { get; set; }

        public NetworkStream Stream { get; set; }

        public static bool Add(ClientCacheModel model)
        {
            return clients.TryAdd(model.RequestId, model);
        }

        public static bool Contains(long id)
        {
            return clients.ContainsKey(id);
        }

        public static bool Get(long id, out ClientCacheModel c)
        {
            return clients.TryGetValue(id, out c);
        }

        public static void Remove(long id)
        {
            if (clients.TryRemove(id, out ClientCacheModel c))
            {
                c.Socket.SafeClose();
                try
                {
                    c.Stream.Close();
                    c.Stream.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }
        public static void Clear(int sourcePort)
        {
            IEnumerable<long> requestIds = clients.Where(c => c.Value.SourcePort == sourcePort).Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                Remove(requestId);
            }
        }

        public void Remove()
        {
            Remove(RequestId);
        }
    }
    public class ServerModel
    {
        public int Port { get; set; } = 0;
        public Socket Socket { get; set; }
        public ManualResetEvent AcceptDone { get; set; }
        public CancellationTokenSource CancelToken { get; set; }

        public static ConcurrentDictionary<int, ServerModel> services = new();

        public static bool Add(ServerModel model)
        {
            return services.TryAdd(model.Port, model);
        }

        public static bool Contains(int id)
        {
            return services.ContainsKey(id);
        }

        public static bool Get(int id, out ServerModel c)
        {
            return services.TryGetValue(id, out c);
        }

        public static void Remove(int id)
        {
            if (services.TryRemove(id, out ServerModel c))
            {
                c.Socket.SafeClose();
                c.CancelToken.Cancel();
                c.AcceptDone.Dispose();
            }
        }
        public static IEnumerable<ServerModel> All()
        {
            return services.Values;
        }

        public void Remove()
        {
            Remove(Port);
        }

        public static void Clear()
        {
            foreach (var item in services)
            {
                Remove(item.Key);
            }
        }
    }

    [ProtoContract, MessagePackObject]
    public class TcpForwardModel
    {
        public TcpForwardModel() { }

        [ProtoMember(1), Key(1)]
        public long RequestId { get; set; } = 0;

        [ProtoMember(2), Key(2)]
        public byte[] Buffer { get; set; } = new byte[0];

        [ProtoMember(3, IsRequired = true), Key(3)]
        public TcpForwardType Type { get; set; } = TcpForwardType.REQUEST;

        [ProtoMember(4), Key(4)]
        public string TargetIp { get; set; } = string.Empty;

        [ProtoMember(5), Key(5)]
        public int TargetPort { get; set; } = 0;

        [ProtoMember(6, IsRequired = true), Key(6)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;

        [ProtoMember(7), Key(7)]
        public byte Compress { get; set; } = 0;
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardType
    {
        REQUEST, RESPONSE, FAIL, RESPONSE_END
    }

    [ProtoContract, MessagePackObject]
    public class TcpForwardRecordBaseModel
    {
        [ProtoMember(2), Key(2)]
        public int ServerPort { get; set; } = 8080;
        [ProtoMember(3), Key(3)]
        public string TargetIp { get; set; } = "127.0.0.1";
        [ProtoMember(4), Key(4)]
        public int TargetPort { get; set; } = 8080;
        [ProtoMember(5), Key(5)]
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
    }

    public class TcpForwardRecordModel : TcpForwardRecordBaseModel
    {
        public RegisterCacheModel Client { get; set; }
        public Dictionary<string, ForwardWebItem> WebForwards { get; set; } = new Dictionary<string, ForwardWebItem>();
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum TcpForwardAliveTypes : int
    {
        TUNNEL,
        //短连接
        WEB
    }

    public class ClientModel
    {
        public long RequestId { get; set; }
        public int SourcePort { get; set; } = 0;
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }
        public byte[] BufferSize { get; set; }
        public string TargetIp { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 0;
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
        public NetworkStream Stream { get; set; }

        private readonly static ConcurrentDictionary<long, ClientModel> clients = new();

        public static bool Add(ClientModel model)
        {
            return clients.TryAdd(model.RequestId, model);
        }

        public static bool Contains(long id)
        {
            return clients.ContainsKey(id);
        }

        public static bool Get(long id, out ClientModel c)
        {
            return clients.TryGetValue(id, out c);
        }

        public static void Remove(long id)
        {
            if (clients.TryRemove(id, out ClientModel c))
            {
                if (c.TargetSocket != null)
                {
                    c.TargetSocket.SafeClose();
                }

                try
                {
                    c.Stream.Close();
                    c.Stream.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        public static void Clear(int sourcePort)
        {
            IEnumerable<long> requestIds = clients.Where(c => c.Value.SourcePort == sourcePort).Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                Remove(requestId);
            }
        }

        public void Remove()
        {
            Remove(RequestId);
        }
    }
}
