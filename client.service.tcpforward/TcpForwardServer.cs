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
        private readonly IClientInfoCaching  clientInfoCaching;
        
        public TcpForwardServer(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }

        private long requestId = 0;
        public event EventHandler<TcpForwardRequestModel> OnRequest;
        public event EventHandler<ListeningChangeModel> OnListeningChange;

        public void Start(TcpForwardRecordModel mapping)
        {
            if (ServerModel.Contains(mapping.SourcePort))
            {
                return;
            }

            Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
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
            OnListeningChange?.Invoke(this, new ListeningChangeModel
            {
                SourcePort = mapping.SourcePort,
                Listening = true
            });

            int sourcePort = mapping.SourcePort;
            int targetPort = mapping.TargetPort;
            string targetIp = mapping.TargetIp;
            ClientInfo targetClient = mapping.Client;
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
                long _requestId = requestId;
                ClientCacheModel.Add(new ClientCacheModel { RequestId = _requestId, SourcePort = server.SourcePort, Socket = socket });

                ClientModel2 client = new()
                {
                    RequestId = _requestId,
                    TargetPort = server.TargetPort,
                    AliveType = server.AliveType,
                    TargetIp = server.TargetIp,
                    SourceSocket = socket,
                    TargetClient = server.TargetClient
                };
                if (server.AliveType == TcpForwardAliveTypes.UNALIVE)
                {
                    var bytes = client.SourceSocket.ReceiveAll();
                    if (bytes.Length > 0)
                    {
                        Receive(client, bytes);
                    }
                    else
                    {
                        ClientCacheModel.Remove(client.RequestId);
                    }
                }
                else
                {
                    BindReceive(client);
                }
            }
            catch (Exception)
            {
                Stop(server.SourcePort);

            }
        }

        public void BindReceive(ClientModel2 client)
        {
            Task.Factory.StartNew((e) =>
            {
                while (client.SourceSocket.Connected && ClientCacheModel.Contains(client.RequestId))
                {
                    try
                    {
                        var bytes = client.SourceSocket.ReceiveAll();
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
            }, TaskCreationOptions.LongRunning);
        }

        private void Receive(ClientModel2 client, byte[] data)
        {
            Socket socket = null;
            if (client.TargetClient != null)
            {
                socket = client.TargetClient.Socket;
                if (socket == null || !socket.Connected || client.TargetClient.Name != client.TargetClient.Name)
                {
                    client.TargetClient = clientInfoCaching.All().FirstOrDefault(c => c.Name == client.TargetClient.Name);
                    if (client.TargetClient != null)
                    {
                        socket = client.TargetClient.Socket;
                    }
                }
            }
            OnRequest?.Invoke(this, new TcpForwardRequestModel
            {
                Msg = new TcpForwardModel
                {
                    RequestId = client.RequestId,
                    Buffer = data,
                    Type = TcpForwardType.REQUEST,
                    TargetPort = client.TargetPort,
                    AliveType = client.AliveType,
                    TargetIp = client.TargetIp
                },
                Socket = socket
            });
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
                    if (client.Socket.Connected)
                    {
                        int length = client.Socket.Send(model.Buffer, SocketFlags.None);
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        public void ResponseEnd(TcpForwardModel model)
        {
            if (ClientCacheModel.Get(model.RequestId, out ClientCacheModel client) && client != null)
            {
                try
                {
                    if (client.Socket.Connected)
                    {
                        int length = client.Socket.Send(model.Buffer, SocketFlags.None);
                    }
                }
                catch (Exception)
                {
                }
                if (model.AliveType == TcpForwardAliveTypes.UNALIVE)
                {
                    client.Remove();
                }
            }
        }

        public void Fail(TcpForwardModel failModel, string body = "snltty")
        {
            if (ClientCacheModel.Get(failModel.RequestId, out ClientCacheModel client) && client != null)
            {
                if (failModel.AliveType == TcpForwardAliveTypes.UNALIVE)
                {
                    try
                    {
                        var bodyBytes = Encoding.UTF8.GetBytes(body);
                        _ = client.Socket.Send(Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n"));
                        _ = client.Socket.Send(Encoding.UTF8.GetBytes("Content-Type: text/html;charset=utf-8\r\n"));
                        _ = client.Socket.Send(Encoding.UTF8.GetBytes($"Content-Length:{bodyBytes.Length}\r\n"));
                        _ = client.Socket.Send(Encoding.UTF8.GetBytes("\r\n"));
                        _ = client.Socket.Send(bodyBytes);
                    }
                    catch (Exception)
                    {
                    }
                }
                if (failModel.Buffer != null)
                {
                    Logger.Instance.Info(failModel.Buffer.Length.ToString());
                }
                client.Remove();
            }
        }

        public void Stop(ServerModel model)
        {
            OnListeningChange?.Invoke(this, new ListeningChangeModel
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
    }


    public class ClientCacheModel
    {
        private static ConcurrentDictionary<long, ClientCacheModel> clients = new();

        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
        public long RequestId { get; set; }

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