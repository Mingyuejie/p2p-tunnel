using client.messengers.clients;
using common;
using common.extends;
using server;
using server.servers.IOCP;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace client.service.tcpforward
{
    public class TcpForwardServer : ITcpForwardServer
    {
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 1;    // read, write (don't alloc buffer space for accepts)
        SocketAsyncEventArgsPool m_readWritePool;
        Semaphore m_maxNumberAcceptedClients;

        public SimpleSubPushHandler<TcpForwardRequestInfo> OnRequest { get; } = new SimpleSubPushHandler<TcpForwardRequestInfo>();
        public SimpleSubPushHandler<ListeningChangeInfo> OnListeningChange { get; } = new SimpleSubPushHandler<ListeningChangeInfo>();

        private readonly IClientInfoCaching clientInfoCaching;
        private NumberSpace requestIdNs = new NumberSpace(0);

        public TcpForwardServer(IClientInfoCaching clientInfoCaching, Config tcpForwardSettingModel)
        {
            this.clientInfoCaching = clientInfoCaching;

            m_numConnections = tcpForwardSettingModel.NumConnections;
            m_receiveBufferSize = tcpForwardSettingModel.ReceiveBufferSize;
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

        public void Start(TcpForwardRecordInfo mapping)
        {
            if (ServerInfo.Contains(mapping.SourcePort))
                return;

            BindAccept(mapping);

            OnListeningChange.Push(new ListeningChangeInfo
            {
                SourcePort = mapping.SourcePort,
                Listening = true
            });
        }

        private void BindAccept(TcpForwardRecordInfo mapping)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(mapping.SourceIp), mapping.SourcePort);

            var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            ServerInfo.Add(new ServerInfo
            {
                SourcePort = mapping.SourcePort,
                Socket = socket
            });

            ClientInfo targetClient = mapping.Client ?? new ClientInfo { Name = mapping.TargetName };
            SocketAsyncEventArgs acceptEventArg = new SocketAsyncEventArgs();
            acceptEventArg.UserToken = new AsyncUserToken
            {
                SourceSocket = socket,
                RequestId = 0,
                TargetPort = mapping.TargetPort,
                AliveType = mapping.AliveType,
                TargetIp = mapping.TargetIp,
                TargetClient = targetClient,
                SourcePort = mapping.SourcePort
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
            AsyncUserToken serverToken = (AsyncUserToken)e.UserToken;
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
            AsyncUserToken token = ((AsyncUserToken)readEventArgs.UserToken);
            token.SourceSocket = e.AcceptSocket;
            token.RequestId = requestIdNs.Get();
            token.TargetPort = serverToken.TargetPort;
            token.AliveType = serverToken.AliveType;
            token.TargetIp = serverToken.TargetIp;
            token.TargetClient = serverToken.TargetClient;
            token.SourcePort = serverToken.SourcePort;

            ClientCacheInfo.Add(token);
            if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }
            StartAccept(e);
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

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.CacheBuffer.AddRange(e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred).ToArray());
                if (token.SourceSocket.Available > 0)
                {
                    var bytes = new byte[token.SourceSocket.Available];
                    token.SourceSocket.Receive(bytes);
                    token.CacheBuffer.AddRange(bytes);
                }
                Receive(e, token.CacheBuffer.ToArray());
                //token.SourceSocket.Send(GetData("response text"));
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
        private byte[] GetData(string body)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("HTTP/1.1 200 OK\r\n");
            sb.Append("Content-Type: text/html;charset=utf-8\r\n");
            sb.Append($"Content-Length: {body.GetBytes().Length}\r\n");
            sb.Append($"Connection: keep-alive\r\n");
            sb.Append("Access-Control-Allow-Credentials: true\r\n");
            sb.Append("Access-Control-Allow-Headers: *\r\n");
            sb.Append("Access-Control-Allow-Methods: *\r\n");
            sb.Append("Access-Control-Allow-Origin: *\r\n");
            sb.Append("\r\n");
            sb.Append(body);

            return sb.ToString().GetBytes();
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

            ClientCacheInfo.Remove(token.RequestId);
            token.SourceSocket.SafeClose();
            token.CacheBuffer.Clear();

            m_readWritePool.Push(e);
            m_maxNumberAcceptedClients.Release();


            IConnection connection = GetSocket(token);
            OnRequest.PushAsync(new TcpForwardRequestInfo
            {
                Msg = new TcpForwardInfo
                {
                    RequestId = token.RequestId,
                    Buffer = Array.Empty<byte>(),
                    Type = TcpForwardTypes.CLOSE,
                    TargetPort = token.TargetPort,
                    AliveType = token.AliveType,
                    TargetIp = token.TargetIp
                },
                Connection = connection
            }).Wait();
        }

        private void Receive(SocketAsyncEventArgs e, byte[] data)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            IConnection connection = GetSocket(token);

            //userToken.SourceSocket.Send(GetData("response text"));
            OnRequest.PushAsync(new TcpForwardRequestInfo
            {
                Msg = new TcpForwardInfo
                {
                    RequestId = token.RequestId,
                    Buffer = data,
                    Type = TcpForwardTypes.REQUEST,
                    TargetPort = token.TargetPort,
                    AliveType = token.AliveType,
                    TargetIp = token.TargetIp,
                    FromID = token.TargetClient.Id
                },
                Connection = connection
            }).Wait();
        }

        private IConnection GetSocket(AsyncUserToken token)
        {
            IConnection connection = null;
            if (token.TargetClient != null)
            {
                connection = token.TargetClient.TcpConnection;
                if (connection == null || !connection.Connected || token.TargetClient.Name != token.TargetClient.Name)
                {
                    var clientinfo = clientInfoCaching.All().FirstOrDefault(c => c.Name == token.TargetClient.Name);
                    if (clientinfo != null)
                    {
                        token.TargetClient = clientinfo;
                        connection = token.TargetClient.TcpConnection;
                    }
                }
            }
            return connection;
        }

        public void Response(TcpForwardInfo model)
        {
            if (ClientCacheInfo.Get(model.RequestId, out AsyncUserToken client) && client != null)
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

        public void Fail(TcpForwardInfo failModel, string body = "")
        {
            ClientCacheInfo.Get(failModel.RequestId, out AsyncUserToken client);
            if (client != null && failModel.AliveType == TcpForwardAliveTypes.WEB)
            {
                byte[] bytes = failModel.Buffer;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    bytes = body.GetBytes();
                }

                StringBuilder sb = new StringBuilder();
                sb.Append("HTTP/1.1 200 OK\r\n");
                sb.Append("Content-Type: text/html;charset=utf-8\r\n");
                sb.Append($"Content-Length: {bytes.Length}\r\n");
                sb.Append("Access-Control-Allow-Credentials: true\r\n");
                sb.Append("Access-Control-Allow-Headers: *\r\n");
                sb.Append("Access-Control-Allow-Methods: *\r\n");
                sb.Append("Access-Control-Allow-Origin: *\r\n");
                sb.Append("\r\n");

                client.SourceSocket.Send(sb.ToString().GetBytes());
                client.SourceSocket.Send(bytes);
            }
        }

        public void Stop(ServerInfo model)
        {
            OnListeningChange.Push(new ListeningChangeInfo
            {
                SourcePort = model.SourcePort,
                Listening = false
            });

            ClientCacheInfo.Clear(model.SourcePort);
            model.Remove();
        }

        public void Stop(int sourcePort)
        {
            if (ServerInfo.Get(sourcePort, out ServerInfo model))
            {
                Stop(model);
            }
        }
    }

    public class AsyncUserToken
    {
        public ClientInfo TargetClient { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();
        public ulong RequestId { get; set; }
        public int SourcePort { get; set; } = 0;
        public Socket SourceSocket { get; set; }
        public byte[] BufferSize { get; set; }
        public string TargetIp { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 0;
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
    }
    public class ListeningChangeInfo
    {
        public int SourcePort { get; set; } = 0;
        public bool Listening { get; set; } = false;
    }

    public class TcpForwardRequestInfo
    {
        public IConnection Connection { get; set; }
        public TcpForwardInfo Msg { get; set; }
    }

    public class ClientCacheInfo
    {
        private static ConcurrentDictionary<ulong, AsyncUserToken> clients = new();

        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
        public ulong RequestId { get; set; }

        public ConditionStopwatch Watch { get; set; }

        public NetworkStream Stream { get; set; }

        public static IEnumerable<ulong> Ids()
        {
            return clients.Keys;
        }

        public static bool Add(AsyncUserToken model)
        {
            return clients.TryAdd(model.RequestId, model);
        }

        public static bool Contains(ulong id)
        {
            return clients.ContainsKey(id);
        }

        public static bool Get(ulong id, out AsyncUserToken c)
        {
            return clients.TryGetValue(id, out c);
        }

        public static void Remove(ulong id)
        {
            if (clients.TryRemove(id, out AsyncUserToken c))
            {
                // c.SourceSocket.SafeClose();
            }
        }
        public static void Clear(int sourcePort)
        {
            IEnumerable<ulong> requestIds = clients.Where(c => c.Value.SourcePort == sourcePort).Select(c => c.Key);
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

    public class ServerInfo
    {
        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }

        public static ConcurrentDictionary<int, ServerInfo> services = new();

        public static bool Add(ServerInfo model)
        {
            return services.TryAdd(model.SourcePort, model);
        }

        public static bool Contains(int port)
        {
            return services.ContainsKey(port);
        }

        public static bool Get(int port, out ServerInfo c)
        {
            return services.TryGetValue(port, out c);
        }

        public static void Remove(int port)
        {
            if (services.TryRemove(port, out ServerInfo c))
            {
                c.Socket.SafeClose();
            }
        }
        public static IEnumerable<ServerInfo> All()
        {
            return services.Values;
        }

        public void Remove()
        {
            Remove(SourcePort);
        }
    }
}
