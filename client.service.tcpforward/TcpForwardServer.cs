using client.plugins.serverPlugins.clients;
using client.service.tcpforward.client;
using common;
using common.extends;
using server.achieves.IOCP;
using server.model;
using server.packet;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

        public SimplePushSubHandler<TcpForwardRequestModel> OnRequest { get; } = new SimplePushSubHandler<TcpForwardRequestModel>();
        public SimplePushSubHandler<ListeningChangeModel> OnListeningChange { get; } = new SimplePushSubHandler<ListeningChangeModel>();

        private readonly IClientInfoCaching clientInfoCaching;
        private readonly TcpForwardSettingModel tcpForwardSettingModel;
        private long requestId = 0;

        public TcpForwardServer(IClientInfoCaching clientInfoCaching, TcpForwardSettingModel tcpForwardSettingModel)
        {
            this.clientInfoCaching = clientInfoCaching;
            this.tcpForwardSettingModel = tcpForwardSettingModel;

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

        public void Start(TcpForwardRecordModel mapping)
        {
            if (ServerModel.Contains(mapping.SourcePort))
                return;

            BindAccept(mapping);

            OnListeningChange.Push(new ListeningChangeModel
            {
                SourcePort = mapping.SourcePort,
                Listening = true
            });
        }

        private void BindAccept(TcpForwardRecordModel mapping)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse(mapping.SourceIp), mapping.SourcePort);

            var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
            socket.Listen(int.MaxValue);

            ServerModel.Add(new ServerModel
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
            _ = Interlocked.Increment(ref requestId);

            var serverToken = (AsyncUserToken)e.UserToken;
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
            var token = ((AsyncUserToken)readEventArgs.UserToken);
            token.SourceSocket = e.AcceptSocket;
            token.RequestId = requestId;
            token.TargetPort = serverToken.TargetPort;
            token.AliveType = serverToken.AliveType;
            token.TargetIp = serverToken.TargetIp;
            token.TargetClient = serverToken.TargetClient;
            token.SourcePort = serverToken.SourcePort;

            ClientCacheModel.Add(token);
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


            Socket socket = GetSocket(token);
            OnRequest.Push(new TcpForwardRequestModel
            {
                Msg = new TcpForwardModel
                {
                    RequestId = token.RequestId,
                    Buffer = Array.Empty<byte>(),
                    Type = TcpForwardType.CLOSE,
                    TargetPort = token.TargetPort,
                    AliveType = token.AliveType,
                    TargetIp = token.TargetIp
                },
                Socket = socket
            });
        }

        private void Receive(SocketAsyncEventArgs e, byte[] data)
        {
            var userToken = (AsyncUserToken)e.UserToken;
            Socket socket = GetSocket(userToken);

            //userToken.SourceSocket.Send(GetData("response text"));
            OnRequest.Push(new TcpForwardRequestModel
            {
                Msg = new TcpForwardModel
                {
                    RequestId = userToken.RequestId,
                    Buffer = data,
                    Type = TcpForwardType.REQUEST,
                    TargetPort = userToken.TargetPort,
                    AliveType = userToken.AliveType,
                    TargetIp = userToken.TargetIp,
                    FromID = userToken.TargetClient.Id
                },
                Socket = socket
            });
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

        private Socket GetSocket(AsyncUserToken token)
        {
            Socket socket = null;
            if (token.TargetClient != null)
            {
                socket = token.TargetClient.Socket;
                if (socket == null || !socket.Connected || token.TargetClient.Name != token.TargetClient.Name)
                {
                    var clientinfo = clientInfoCaching.All().FirstOrDefault(c => c.Name == token.TargetClient.Name);
                    if (clientinfo != null)
                    {
                        token.TargetClient = clientinfo;
                        socket = token.TargetClient.Socket;
                    }
                }
            }
            return socket;
        }

        public void Response(TcpForwardModel model)
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

        public void Fail(TcpForwardModel failModel, string body = "")
        {
            ClientCacheModel.Get(failModel.RequestId, out AsyncUserToken client);
            if (client != null && failModel.AliveType == TcpForwardAliveTypes.WEB)
            {
                StringBuilder sb = new StringBuilder();
                byte[] bytes = failModel.Buffer;
                if (body.Length > 0)
                {
                    bytes = Encoding.UTF8.GetBytes(body);
                }

                sb.Append("HTTP/1.1 200 OK\r\n");
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
        }

        public void Stop(ServerModel model)
        {
            OnListeningChange.Push(new ListeningChangeModel
            {
                SourcePort = model.SourcePort,
                Listening = false
            });

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
    }

    public class AsyncUserToken
    {
        public ClientInfo TargetClient { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();
        public long RequestId { get; set; }
        public int SourcePort { get; set; } = 0;
        public Socket SourceSocket { get; set; }
        public byte[] BufferSize { get; set; }
        public string TargetIp { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 0;
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
    }
    public class ListeningChangeModel
    {
        public int SourcePort { get; set; } = 0;
        public bool Listening { get; set; } = false;
    }

    public class TcpForwardRequestModel
    {
        public Socket Socket { get; set; }
        public TcpForwardModel Msg { get; set; }
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
}
