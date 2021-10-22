using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
using server.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverTcpforward
{
    public class ServerTcpForwardHelper
    {
        private readonly IServerRequest serverRequest;
        private readonly RegisterState registerstate;
        private readonly ServerTcpForwardRegisterConfig serverTcpForwardRegisterConfig;
        public ServerTcpForwardHelper(IServerRequest serverRequest, RegisterState registerstate, ServerTcpForwardRegisterConfig serverTcpForwardRegisterConfig)
        {
            this.serverRequest = serverRequest;
            this.registerstate = registerstate;
            this.serverTcpForwardRegisterConfig = serverTcpForwardRegisterConfig;
        }

        public ServerMessageResponeWrap Register()
        {
            var res = serverRequest.SendReplyTcp(new SendTcpEventArg<ServerTcpForwardRegisterModel>
            {
                Data = new ServerTcpForwardRegisterModel
                {
                    Id = registerstate.RemoteInfo.ConnectId,
                    Web = serverTcpForwardRegisterConfig.Web,
                    Tunnel = serverTcpForwardRegisterConfig.Tunnel
                },
                Socket = registerstate.TcpSocket,
                Path = "ServerTcpForward/register"
            }).Result;
            if (res.Code == ServerMessageResponeCodes.OK)
            {
                Logger.Instance.Info("已注册服务器TCP转发");
            }
            else
            {
                Logger.Instance.Error($"服务器TCP转发注册失败:{res.ErrorMsg}");
            }
            return res;
        }
        public ServerMessageResponeWrap UnRegister()
        {
            var res = serverRequest.SendReplyTcp(new SendTcpEventArg<ServerTcpForwardRegisterModel>
            {
                Data = new ServerTcpForwardRegisterModel
                {
                    Id = registerstate.RemoteInfo.ConnectId,
                    Web = serverTcpForwardRegisterConfig.Web,
                    Tunnel = serverTcpForwardRegisterConfig.Tunnel
                },
                Socket = registerstate.TcpSocket,
                Path = "ServerTcpForward/unregister"
            }).Result;
            return res;
        }

        public void Request(ServerTcpForwardModel data)
        {
            if (data.Type == ServerTcpForwardType.CLOSE)
            {
                ClientModel.Remove(data.RequestId);
                return;
            }

            if (!serverTcpForwardRegisterConfig.Enable)
            {
                serverRequest.SendOnlyTcp(new SendTcpEventArg<ServerTcpForwardModel>
                {
                    Data = new ServerTcpForwardModel
                    {
                        RequestId = data.RequestId,
                        Type = ServerTcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes("客户端未开启插件"),
                        AliveType = data.AliveType
                    },
                    Socket = registerstate.TcpSocket,
                    Path = "ServerTcpForward/response"
                });
                return;
            }

            ClientModel.Get(data.RequestId, out ClientModel client);
            try
            {
                if (client == null)
                {
                    Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client = new ClientModel
                    {
                        RequestId = data.RequestId,
                        TargetSocket = socket,
                        TargetPort = data.TargetPort,
                        AliveType = data.AliveType,
                        TargetIp = data.TargetIp,
                        SourceSocket = registerstate.TcpSocket
                    };

                    IPEndPoint dnsEndPoint = new(Helper.GetDomainIp(data.TargetIp), data.TargetPort);
                    socket.BeginConnect(dnsEndPoint, new AsyncCallback(Connect), new ConnectState
                    {
                        Client = client,
                        Data = data.Buffer
                    });

                }
                else
                {
                    if (client.TargetSocket.Connected)
                    {
                        client.Stream.Write(data.Buffer);
                        client.Stream.Flush();
                    }
                }

            }
            catch (Exception ex)
            {
                ClientModel.Remove(data.RequestId);
                serverRequest.SendOnlyTcp(new SendTcpEventArg<ServerTcpForwardModel>
                {
                    Data = new ServerTcpForwardModel
                    {
                        RequestId = data.RequestId,
                        Type = ServerTcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes(ex + ""),
                        AliveType = data.AliveType
                    },
                    Socket = registerstate.TcpSocket,
                    Path = "ServerTcpForward/response"
                });
            }
        }

        private void Connect(IAsyncResult result)
        {
            var state = (ConnectState)result.AsyncState;
            state.Client.TargetSocket.EndConnect(result);
            result.AsyncWaitHandle.Close();
            state.Client.Stream = new NetworkStream(state.Client.TargetSocket, false);

            ClientModel.Add(state.Client);
            BindReceive(state.Client);

            if (state.Client.TargetSocket.Connected)
            {
                state.Client.Stream.Write(state.Data);
                state.Client.Stream.Flush();

                state.Data = Array.Empty<byte>();
            }
        }

        private void BindReceive(ClientModel client)
        {
            _ = Task.Run(() =>
            {
                while (client.Stream.CanRead && ClientModel.Contains(client.RequestId))
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
                            client.Remove();
                        }
                    }
                    catch (Exception)
                    {
                        client.Remove();
                        break;
                    }
                }
                client.Remove();
            });
        }

        private void Receive(ClientModel client, byte[] data)
        {
            serverRequest.SendOnlyTcp(new SendTcpEventArg<ServerTcpForwardModel>
            {
                Data = new ServerTcpForwardModel
                {
                    RequestId = client.RequestId,
                    Buffer = data,
                    Type = ServerTcpForwardType.RESPONSE,
                    TargetPort = client.TargetPort,
                    AliveType = client.AliveType,
                    TargetIp = client.TargetIp
                },
                Socket = registerstate.TcpSocket,
                Path = "ServerTcpForward/response"
            });
        }
    }

    public class ConnectState
    {
        public ClientModel Client { get; set; }
        public byte[] Data { get; set; }
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
        public ServerTcpForwardAliveTypes AliveType { get; set; } = ServerTcpForwardAliveTypes.WEB;
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