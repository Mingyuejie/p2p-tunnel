using common;
using common.extends;
using MessagePack;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.achieves.IOCP;
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
using System.Threading;
using System.Threading.Tasks;

namespace server.service.plugins
{
    /// <summary>
    /// TCP代理转发
    /// </summary>
    public class ServerTcpForwardPlugin : IPlugin
    {
        private readonly TcpForwardServer tcpForwardServer;
        private readonly Config config;
        private readonly IClientRegisterCaching clientRegisterCache;

        public ServerTcpForwardPlugin(TcpForwardServer tcpForwardServer, Config config, IClientRegisterCaching clientRegisterCache)
        {
            this.tcpForwardServer = tcpForwardServer;
            this.config = config;
            this.clientRegisterCache = clientRegisterCache;
        }

        public bool Register(PluginParamWrap data)
        {
            if (!config.tcpForward)
            {
                data.SetCode(ServerMessageResponeCodes.ACCESS, "服务端未开启TCP转发");
                return false;
            }
            ServerTcpForwardRegisterModel foreards = data.Wrap.Memory.DeBytes<ServerTcpForwardRegisterModel>();
            if (!clientRegisterCache.Verify(foreards.Id, data))
            {
                data.SetCode(ServerMessageResponeCodes.ACCESS, "认证失败");
                return false;
            }
            RegisterCacheModel source = clientRegisterCache.Get(foreards.Id);
            if (source == null)
            {
                data.SetCode(ServerMessageResponeCodes.ACCESS, "未注册");
                return false;
            }

            try
            {
                tcpForwardServer.StopAll();
                foreach (var webs in foreards.Web)
                {
                    tcpForwardServer.Start(new TcpForwardRecordModel
                    {
                        AliveType = ServerTcpForwardAliveTypes.WEB,
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
                        AliveType = ServerTcpForwardAliveTypes.TUNNEL,
                        Client = source,
                        ServerPort = tunnels.Port,
                        TargetIp = tunnels.TargetIp,
                        TargetPort = tunnels.TargetPort,

                    });
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex + "");
                data.SetCode(ServerMessageResponeCodes.BAD_GATEWAY, ex.Message);
            }
            return true;
        }
        public bool UnRegister(PluginParamWrap data)
        {
            tcpForwardServer.StopAll();
            return true;
        }
        public void Response(PluginParamWrap data)
        {
            var model = data.Wrap.Memory.DeBytes<ServerTcpForwardModel>();
            if (model.Type == ServerTcpForwardType.RESPONSE)
            {
                tcpForwardServer.Response(model);
            }
            else
            {
                tcpForwardServer.Fail(model);
            }
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
            Logger.Instance.Info("TCP转发服务已开启");
            return obj;
        }
    }

    public class TcpForwardServer
    {
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 1;    // read, write (don't alloc buffer space for accepts)
        SocketAsyncEventArgsPool m_readWritePool;
        Semaphore m_maxNumberAcceptedClients;

        private readonly ServerPluginHelper serverPluginHelper;
        private readonly IClientRegisterCaching clientRegisterCache;
        private long requestId = 0;

        public TcpForwardServer(ServerPluginHelper serverPluginHelper, IClientRegisterCaching clientRegisterCache)
        {
            this.serverPluginHelper = serverPluginHelper;
            this.clientRegisterCache = clientRegisterCache;

            m_numConnections = 3000;
            m_receiveBufferSize = 1024;
            m_bufferManager = new BufferManager(m_receiveBufferSize * m_numConnections * opsToPreAlloc,
                m_receiveBufferSize);

            m_readWritePool = new SocketAsyncEventArgsPool(m_numConnections);
            m_maxNumberAcceptedClients = new Semaphore(m_numConnections, m_numConnections);

            m_bufferManager.InitBuffer();

            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += IO_Completed;
                readWriteEventArg.UserToken = new AsyncUserToken();

                m_bufferManager.SetBuffer(readWriteEventArg);

                m_readWritePool.Push(readWriteEventArg);
            }
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    Logger.Instance.Error(e.LastOperation.ToString());
                    break;
            }
        }

        public void Start(TcpForwardRecordModel mapping)
        {
            if (ServerModel.Contains(mapping.ServerPort))
                return;

            BindAccept(mapping);
        }
        private void BindAccept(TcpForwardRecordModel mapping)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, mapping.ServerPort);

            var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            ServerModel.Add(new ServerModel
            {
                SourcePort = mapping.ServerPort,
                Socket = socket
            });

            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.UserToken = new AsyncUserToken
            {
                SourceSocket = socket,
                RequestId = 0,
                TargetPort = mapping.TargetPort,
                AliveType = mapping.AliveType,
                TargetIp = mapping.TargetIp,
                SourcePort = mapping.ServerPort,
                WebForwards = mapping.WebForwards,
                TargetClientId = mapping.Client.Id
            };
            acceptEventArg.Completed += IO_Accept;
            StartAccept(acceptEventArg);
        }
        private void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            try
            {
                acceptEventArg.AcceptSocket = null;
                AsyncUserToken token = ((AsyncUserToken)acceptEventArg.UserToken);
                m_maxNumberAcceptedClients.WaitOne();
                if (!token.SourceSocket.AcceptAsync(acceptEventArg))
                {
                    ProcessAccept(acceptEventArg);
                }
            }
            catch (Exception)
            {
            }
        }
        private void IO_Accept(object sender, SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            _ = Interlocked.Increment(ref requestId);

            var serverToken = (AsyncUserToken)e.UserToken;
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
            var token = ((AsyncUserToken)readEventArgs.UserToken);
            token.SourceSocket = e.AcceptSocket;
            token.RequestId = requestId;
            token.TargetPort = serverToken.TargetPort;
            token.AliveType = serverToken.AliveType;
            token.TargetIp = serverToken.TargetIp;
            token.WebForwards = serverToken.WebForwards;
            token.SourcePort = serverToken.SourcePort;
            token.TargetClientId = serverToken.TargetClientId;

            ClientCacheModel.Add(token);
            if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }
            StartAccept(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, data.Length);
                token.CacheBuffer.AddRange(data);

                if (token.SourceSocket.Available > 0)
                {
                    var bytes = new byte[token.SourceSocket.Available];
                    token.SourceSocket.Receive(bytes);
                    token.CacheBuffer.AddRange(bytes);
                }
                Receive(e, token.CacheBuffer.ToArray());
                token.CacheBuffer.Clear();
                if (!token.SourceSocket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {

            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            ClientCacheModel.Remove(token.RequestId);
            token.SourceSocket.SafeClose();
            token.CacheBuffer.Clear();

            m_readWritePool.Push(e);
            m_maxNumberAcceptedClients.Release();

            if (token.TargetPort != 0)
            {
                var registerInfo = clientRegisterCache.Get(token.TargetClientId);
                SendMessageWrap<ServerTcpForwardModel> model = new SendMessageWrap<ServerTcpForwardModel>
                {
                    Code = ServerMessageResponeCodes.OK,
                    Path = "ServerTcpForward/excute",
                    RequestId = token.RequestId,
                    TcpCoket = registerInfo?.TcpSocket,
                    Type = ServerMessageTypes.REQUEST,
                    Data = new ServerTcpForwardModel
                    {
                        RequestId = token.RequestId,
                        Buffer = Array.Empty<byte>(),
                        Type = ServerTcpForwardType.CLOSE,
                        TargetPort = token.TargetPort,
                        AliveType = token.AliveType,
                        TargetIp = token.TargetIp
                    },
                };
                serverPluginHelper.SendOnlyTcp(model);
            }
        }

        private byte[] GetData(string body)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 200 OK\r\n");
            sb.Append("Content-Type: text/html;charset=utf-8\r\n");
            sb.Append($"Content-Length: {Encoding.UTF8.GetBytes(body).Length}\r\n");
            sb.Append($"Connection: keep-alive\r\n");
            sb.Append("Access-Control-Allow-Credentials: true\r\n");
            sb.Append("Access-Control-Allow-Headers: *\r\n");
            sb.Append("Access-Control-Allow-Methods: *\r\n");
            sb.Append("Access-Control-Allow-Origin: *\r\n");
            sb.Append("\r\n");
            sb.Append(body);

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        private void Receive(SocketAsyncEventArgs e, byte[] data)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            //token.SourceSocket.Send(GetData("reponse text"));
            // return;

            //web的时候，根据请求的域名 指向不同的目标ip和端口，所以需要解析一下 request headers 拿到host
            if (token.AliveType == ServerTcpForwardAliveTypes.WEB && token.TargetPort == 0)
            {
                string host = GetHost(data);
                if (token.WebForwards.ContainsKey(host))
                {
                    var item = token.WebForwards[host];
                    token.TargetPort = item.TargetPort;
                    token.TargetIp = item.TargetIp;
                }
            }

            var registerInfo = clientRegisterCache.Get(token.TargetClientId);
            SendMessageWrap<ServerTcpForwardModel> model = new SendMessageWrap<ServerTcpForwardModel>
            {
                Code = ServerMessageResponeCodes.OK,
                Path = "ServerTcpForward/excute",
                RequestId = token.RequestId,
                TcpCoket = registerInfo?.TcpSocket,
                Type = ServerMessageTypes.REQUEST,
                Data = new ServerTcpForwardModel
                {
                    RequestId = token.RequestId,
                    Buffer = data,
                    Type = ServerTcpForwardType.REQUEST,
                    TargetPort = token.TargetPort,
                    AliveType = token.AliveType,
                    TargetIp = token.TargetIp
                },
            };
            if (token.TargetPort == 0)
            {
                Fail(model.Data, "未选择转发对象，或者未与转发对象建立连接1");
                return;
            }
            if (token.SourceSocket == null || !token.SourceSocket.Connected)
            {
                Fail(model.Data, "未选择转发对象，或者未与转发对象建立连接1");
                return;
            }
            serverPluginHelper.SendOnlyTcp(model);
        }

        public void Response(ServerTcpForwardModel model)
        {
            if (ClientCacheModel.Get(model.RequestId, out AsyncUserToken client) && client != null)
            {
                try
                {
                    client.SourceSocket.Send(model.Buffer);
                }
                catch (Exception)
                {
                }
            }
        }
        public void Fail(ServerTcpForwardModel failModel, string body = "")
        {
            if (ClientCacheModel.Get(failModel.RequestId, out AsyncUserToken client) && client != null)
            {
                if (failModel.AliveType == ServerTcpForwardAliveTypes.WEB)
                {
                    StringBuilder sb = new StringBuilder();

                    byte[] bytes = Array.Empty<byte>();
                    if (!string.IsNullOrWhiteSpace(body))
                    {
                        bytes = Encoding.UTF8.GetBytes(body);
                    }
                    else if (failModel.Buffer != null && failModel.Buffer.Length > 0)
                    {
                        bytes = failModel.Buffer;
                    }

                    if (failModel.Buffer.IsOptionsMethod())
                    {
                        sb.Append("HTTP/1.1 204 No Content\r\n");
                    }
                    else
                    {
                        sb.Append("HTTP/1.1 404 Not Found\r\n");
                    }
                    sb.Append("Content-Type: text/html;charset=utf-8\r\n");
                    sb.Append($"Content-Length: {bytes.Length}\r\n");
                    sb.Append("Access-Control-Allow-Credentials: true\r\n");
                    sb.Append("Access-Control-Allow-Headers: *\r\n");
                    sb.Append("Access-Control-Allow-Methods: *\r\n");
                    sb.Append("Access-Control-Allow-Origin: *\r\n");
                    sb.Append("\r\n");
                    client.SourceSocket.Send(Encoding.UTF8.GetBytes(sb.ToString()));
                    client.SourceSocket.Send(bytes);
                }
                else
                {
                    Logger.Instance.Error(Encoding.UTF8.GetString(failModel.Buffer));
                }
            }
        }
        public void Stop(ServerModel model)
        {
            ClientCacheModel.Clear(model.SourcePort);
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
        private byte[] endBytes = Encoding.ASCII.GetBytes("\r\n");
        private string GetHost(byte[] bytes)
        {
            Span<byte> span = bytes.AsSpan();
            int index = span.IndexOf(hostBytes) + hostBytes.Length;
            if (index < 0)
            {
                return string.Empty;
            }
            int index2 = span.Slice(index).IndexOf(endBytes) + index;
            return Encoding.UTF8.GetString(span.Slice(index + 1, index2 - (index + 1)));
        }
    }

    public class AsyncUserToken
    {
        public Dictionary<string, ServerTcpForwardWebItem> WebForwards { get; set; } = new Dictionary<string, ServerTcpForwardWebItem>();
        public List<byte> CacheBuffer { get; set; } = new List<byte>();
        public long RequestId { get; set; }
        public int SourcePort { get; set; } = 0;
        public Socket SourceSocket { get; set; }
        public byte[] BufferSize { get; set; }
        public string TargetIp { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 0;
        public long TargetClientId { get; set; }
        public ServerTcpForwardAliveTypes AliveType { get; set; } = ServerTcpForwardAliveTypes.WEB;
    }

    public class ClientCacheModel
    {
        private static ConcurrentDictionary<long, AsyncUserToken> clients = new();

        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
        public long RequestId { get; set; }

        public MyStopwatch Watch { get; set; }

        public NetworkStream Stream { get; set; }

        public static IEnumerable<long> Ids()
        {
            return clients.Keys;
        }

        public static bool Add(AsyncUserToken model)
        {
            return clients.TryAdd(model.RequestId, model);
        }

        public static bool Contains(long id)
        {
            return clients.ContainsKey(id);
        }

        public static bool Get(long id, out AsyncUserToken c)
        {
            return clients.TryGetValue(id, out c);
        }

        public static void Remove(long id)
        {
            if (clients.TryRemove(id, out AsyncUserToken c))
            {
                // c.SourceSocket.SafeClose();
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

        public static int Count()
        {
            return clients.Count;
        }
    }
    public class ServerModel
    {
        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }

        public static ConcurrentDictionary<int, ServerModel> services = new();

        public static bool Add(ServerModel model)
        {
            return services.TryAdd(model.SourcePort, model);
        }

        public static bool Contains(int port)
        {
            return services.ContainsKey(port);
        }

        public static bool Get(int port, out ServerModel c)
        {
            return services.TryGetValue(port, out c);
        }

        public static void Remove(int port)
        {
            if (services.TryRemove(port, out ServerModel c))
            {
                c.Socket.SafeClose();
            }
        }
        public static IEnumerable<ServerModel> All()
        {
            return services.Values;
        }

        public void Remove()
        {
            Remove(SourcePort);
        }
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
        public ServerTcpForwardAliveTypes AliveType { get; set; } = ServerTcpForwardAliveTypes.WEB;
    }

    public class TcpForwardRecordModel : TcpForwardRecordBaseModel
    {
        public RegisterCacheModel Client { get; set; }
        public Dictionary<string, ServerTcpForwardWebItem> WebForwards { get; set; } = new Dictionary<string, ServerTcpForwardWebItem>();
    }
}
