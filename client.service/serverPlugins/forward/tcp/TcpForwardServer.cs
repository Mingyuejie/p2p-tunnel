using client.service.serverPlugins.clients;
using common;
using common.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.serverPlugins.forward.tcp
{
    internal class TcpForwardServer
    {
        private static readonly Lazy<TcpForwardServer> lazy = new(() => new TcpForwardServer());
        public static TcpForwardServer Instance => lazy.Value;

        private TcpForwardServer()
        {
        }

        private long requestId = 0;
        public event EventHandler<TcpForwardRequestModel> OnRequest;
        public ConcurrentDictionary<long, ClientCacheModel> clients = new();
        public ConcurrentDictionary<int, ServerModel> services = new();

        public event EventHandler<ListeningChangeModel> OnListeningChange;

        public void Start(TcpForwardRecordModel mapping)
        {
            if (services.ContainsKey(mapping.SourcePort))
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
            _ = services.TryAdd(mapping.SourcePort, server);
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
                        services.TryRemove(sourcePort, out _);
                        server.AcceptDone.Dispose();
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
                _ = clients.TryAdd(_requestId, new ClientCacheModel { SourcePort = server.SourcePort, Socket = socket });

                ClientModel2 client = new()
                {
                    RequestId = _requestId,
                    TargetPort = server.TargetPort,
                    AliveType = server.AliveType,
                    TargetIp = server.TargetIp,
                    SourceSocket = socket,
                    TargetClient = server.TargetClient
                };
                BindReceive(client);
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
                while (client.SourceSocket.Connected && clients.ContainsKey(client.RequestId))
                {
                    try
                    {
                        Receive(client, client.SourceSocket.ReceiveAll());
                    }
                    catch (Exception)
                    {
                        _ = clients.TryRemove(client.RequestId, out _);
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
                if (socket == null || !socket.Connected)
                {
                    client.TargetClient = AppShareData.Instance.Clients.Values.FirstOrDefault(c => c.Name == client.TargetClient.Name);
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
            if (clients.TryGetValue(model.RequestId, out ClientCacheModel client) && client != null)
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
            if (clients.TryGetValue(model.RequestId, out ClientCacheModel client) && client != null)
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
                    client.Socket.SafeClose();
                    _ = clients.TryRemove(model.RequestId, out _);
                }
            }
        }

        public void Fail(TcpForwardModel failModel, string body = "snltty")
        {
            if (clients.TryRemove(failModel.RequestId, out ClientCacheModel client) && client != null)
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
                client.Socket.SafeClose();
            }
        }

        public void Stop(int sourcePort)
        {
            if (services.TryRemove(sourcePort, out ServerModel server) && server != null)
            {
                server.Socket.SafeClose();
                server.CancelToken.Cancel();
                server.AcceptDone.Dispose();
            }

            OnListeningChange?.Invoke(this, new ListeningChangeModel
            {
                SourcePort = sourcePort,
                Listening = false
            });

            IEnumerable<long> requestIds = clients.Where(c => c.Value.SourcePort == sourcePort).Select(c => c.Key);
            foreach (var requestId in requestIds)
            {
                if (clients.TryRemove(requestId, out ClientCacheModel client) && client != null)
                {
                    client.Socket.SafeClose();
                }
            }
        }
        public void StopAll()
        {
            foreach (KeyValuePair<int, ServerModel> item in services)
            {
                Stop(item.Key);
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
        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
    }
    public class ServerModel
    {
        public int SourcePort { get; set; } = 0;
        public Socket Socket { get; set; }
        public ManualResetEvent AcceptDone { get; set; }
        public CancellationTokenSource CancelToken { get; set; }
    }
}
