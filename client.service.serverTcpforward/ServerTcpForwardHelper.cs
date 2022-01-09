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

        public async Task<MessageRequestResponeWrap> Register()
        {
            return await serverRequest.SendReply(new SendEventArg<ServerTcpForwardRegisterModel>
            {
                Data = new ServerTcpForwardRegisterModel
                {
                    Web = serverTcpForwardRegisterConfig.Web,
                    Tunnel = serverTcpForwardRegisterConfig.Tunnel
                },
                Connection = registerstate.TcpConnection,
                Path = "ServerTcpForward/register"
            });
        }
        public async Task<MessageRequestResponeWrap> UnRegister()
        {
            return await serverRequest.SendReply(new SendEventArg<ServerTcpForwardRegisterModel>
            {
                Data = new ServerTcpForwardRegisterModel
                {
                    Web = serverTcpForwardRegisterConfig.Web,
                    Tunnel = serverTcpForwardRegisterConfig.Tunnel
                },
                Connection = registerstate.TcpConnection,
                Path = "ServerTcpForward/unregister"
            });
        }

        public async Task Request(ServerTcpForwardModel data)
        {
            if (data.Type == ServerTcpForwardType.CLOSE)
            {
                ClientModel.Remove(data.RequestId);
                return;
            }

            if (!serverTcpForwardRegisterConfig.Enable)
            {
                await serverRequest.SendOnly(new SendEventArg<ServerTcpForwardModel>
                {
                    Data = new ServerTcpForwardModel
                    {
                        RequestId = data.RequestId,
                        Type = ServerTcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes("客户端未开启插件"),
                        AliveType = data.AliveType
                    },
                    Connection = registerstate.TcpConnection,
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
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    client = new ClientModel
                    {
                        RequestId = data.RequestId,
                        TargetSocket = socket,
                        TargetPort = data.TargetPort,
                        AliveType = data.AliveType,
                        TargetIp = data.TargetIp,
                        SourceSocket = registerstate.TcpConnection.TcpSocket
                    };

                    IPEndPoint dnsEndPoint = new(Helper.GetDomainIp(data.TargetIp), data.TargetPort);
                    socket.Connect(dnsEndPoint);
                    ClientModel.Add(client);
                    client.Stream = new NetworkStream(client.TargetSocket, false);

                    ClientModel.Add(client);

                    BindReceive(client);
                }

                if (client.TargetSocket.Connected)
                {
                    client.Stream.Write(data.Buffer);
                    client.Stream.Flush();
                }

            }
            catch (Exception ex)
            {
                ClientModel.Remove(data.RequestId);
                await serverRequest.SendOnly(new SendEventArg<ServerTcpForwardModel>
                {
                    Data = new ServerTcpForwardModel
                    {
                        RequestId = data.RequestId,
                        Type = ServerTcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes(ex.ToString()),
                        AliveType = data.AliveType
                    },
                    Connection = registerstate.TcpConnection,
                    Path = "ServerTcpForward/response"
                });
            }
        }

        private void BindReceive(ClientModel client)
        {
            Task.Run(async () =>
            {
                while (client.Stream.CanRead && ClientModel.Contains(client.RequestId))
                {
                    try
                    {
                        var bytes = client.Stream.ReceiveAll();
                        if (bytes.Length > 0)
                        {
                            await Receive(client, bytes);
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
                client.Remove();
            });
        }

        private async Task Receive(ClientModel client, byte[] data)
        {
            await serverRequest.SendOnly(new SendEventArg<ServerTcpForwardModel>
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
                Connection = registerstate.TcpConnection,
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
        public ulong RequestId { get; set; }
        public int SourcePort { get; set; } = 0;
        public Socket SourceSocket { get; set; }
        public Socket TargetSocket { get; set; }
        public byte[] BufferSize { get; set; }
        public string TargetIp { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 0;
        public ServerTcpForwardAliveTypes AliveType { get; set; } = ServerTcpForwardAliveTypes.WEB;
        public NetworkStream Stream { get; set; }

        private readonly static ConcurrentDictionary<ulong, ClientModel> clients = new();

        public static bool Add(ClientModel model)
        {
            return clients.TryAdd(model.RequestId, model);
        }

        public static bool Contains(ulong id)
        {
            return clients.ContainsKey(id);
        }

        public static bool Get(ulong id, out ClientModel c)
        {
            return clients.TryGetValue(id, out c);
        }

        public static void Remove(ulong id)
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
    }
}