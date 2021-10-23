using client.plugins.serverPlugins.clients;
using common;
using common.extends;
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
    public class TcpForwardServer
    {
        private readonly IClientInfoCaching clientInfoCaching;

        public TcpForwardServer(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        private long requestId = 0;

        public SimplePushSubHandler<TcpForwardRequestModel> OnRequest { get; } = new SimplePushSubHandler<TcpForwardRequestModel>();
        public SimplePushSubHandler<ListeningChangeModel> OnListeningChange { get; } = new SimplePushSubHandler<ListeningChangeModel>();

        public void Start(TcpForwardRecordModel mapping)
        {

            if (ServerModel.Contains(mapping.SourcePort))
            {
                return;
            }

            Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.Bind(new IPEndPoint(IPAddress.Any, mapping.SourcePort));
            socket.Listen(int.MaxValue);


            ServerModel server = new ServerModel
            {
                CancelToken = new CancellationTokenSource(),
                AcceptDone = new ManualResetEvent(false),
                Socket = socket,
                SourcePort = mapping.SourcePort
            };
            ServerModel.Add(server);
            OnListeningChange.Push(new ListeningChangeModel
            {
                SourcePort = mapping.SourcePort,
                Listening = true
            });

            int sourcePort = mapping.SourcePort;
            int targetPort = mapping.TargetPort;
            string targetIp = mapping.TargetIp;
            ClientInfo targetClient = mapping.Client ?? new ClientInfo { Name = mapping.TargetName };
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
                            AcceptDone = server.AcceptDone
                        });
                        _ = server.AcceptDone.WaitOne();
                    }
                    catch (Exception)
                    {
                        Stop(sourcePort);
                        server.Remove();
                        break;
                    }
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
                    Stream = client.Stream
                };
                BindReceive(client1);
            }
            catch (Exception)
            {
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
                            break;
                        }

                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
                ClientCacheModel.Remove(client.RequestId);
                Socket socket = GetSocket(client);
                OnRequest.Push(new TcpForwardRequestModel
                {
                    Msg = new TcpForwardModel
                    {
                        RequestId = client.RequestId,
                        Buffer = Array.Empty<byte>(),
                        Type = TcpForwardType.CLOSE,
                        TargetPort = client.TargetPort,
                        AliveType = client.AliveType,
                        TargetIp = client.TargetIp
                    },
                    Socket = socket
                });

            });
        }
        private void Receive(ClientModel2 client, byte[] data)
        {
            Socket socket = GetSocket(client);
            OnRequest.Push(new TcpForwardRequestModel
            {
                Msg = new TcpForwardModel
                {
                    RequestId = client.RequestId,
                    Buffer = data,
                    Type = TcpForwardType.REQUEST,
                    TargetPort = client.TargetPort,
                    AliveType = client.AliveType,
                    TargetIp = client.TargetIp,
                    FromID = client.TargetClient.Id
                },
                Socket = socket
            });
        }

        private Socket GetSocket(ClientModel2 client)
        {

            Socket socket = null;
            if (client.TargetClient != null)
            {
                socket = client.TargetClient.Socket;
                if (socket == null || !socket.Connected || client.TargetClient.Name != client.TargetClient.Name)
                {
                    var clientinfo = clientInfoCaching.All().FirstOrDefault(c => c.Name == client.TargetClient.Name);
                    if (clientinfo != null)
                    {
                        client.TargetClient = clientinfo;
                        socket = client.TargetClient.Socket;
                    }
                }
            }
            return socket;
        }

        public void StartAll(List<TcpForwardRecordBaseModel> mappings)
        {
            foreach (TcpForwardRecordBaseModel item in mappings)
            {
                Start(new TcpForwardRecordModel
                {
                    AliveType = item.AliveType,
                    SourceIp = item.SourceIp,
                    SourcePort = item.SourcePort,
                    TargetIp = item.TargetIp,
                    TargetName = item.TargetName,
                    TargetPort = item.TargetPort,
                    Listening = false,
                });
            }
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
                    sb.Append("Connection: close\r\n");
                    sb.Append("Access-Control-Allow-Headers: *\r\n");
                    sb.Append("Access-Control-Allow-Methods: *\r\n");
                    sb.Append("Access-Control-Allow-Origin: *\r\n");
                    sb.Append("\r\n");
                    client.Stream.Write(Encoding.UTF8.GetBytes(sb.ToString()));
                    client.Stream.Write(bytes);
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

        public void StopAll()
        {
            foreach (ServerModel item in ServerModel.All())
            {
                Stop(item);
            }
        }
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

    public class ClientModel2 : ClientModel
    {
        public ClientInfo TargetClient { get; set; }
        public ManualResetEvent AcceptDone { get; set; }
        public MyStopwatch Watch { get; set; }
    }


    public class ClientCacheModel
    {
        private static ConcurrentDictionary<long, ClientCacheModel> clients = new();

        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
        public long RequestId { get; set; }

        public MyStopwatch Watch { get; set; }

        public NetworkStream Stream { get; set; }

        public static IEnumerable<long> Ids()
        {
            return clients.Keys;
        }

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

        public static int Count()
        {
            return clients.Count;
        }
    }
    public class ServerModel
    {
        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
        public ManualResetEvent AcceptDone { get; set; }
        public CancellationTokenSource CancelToken { get; set; }

        public static ConcurrentDictionary<int, ServerModel> services = new();

        public static bool Add(ServerModel model)
        {
            return services.TryAdd(model.SourcePort, model);
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
            Remove(SourcePort);
        }
    }
}