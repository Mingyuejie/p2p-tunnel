using client.messengers.register;
using common;
using common.extends;
using server;
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
        private readonly MessengerSender messengerSender;
        private readonly RegisterStateInfo registerstate;
        private readonly Config serverTcpForwardRegisterConfig;
        public ServerTcpForwardHelper(MessengerSender messengerSender, RegisterStateInfo registerstate, Config serverTcpForwardRegisterConfig)
        {
            this.messengerSender = messengerSender;
            this.registerstate = registerstate;
            this.serverTcpForwardRegisterConfig = serverTcpForwardRegisterConfig;
        }

        public async Task<MessageResponeInfo> Register()
        {
            return await messengerSender.SendReply(new MessageRequestParamsInfo<ServerTcpForwardRegisterParamsInfo>
            {
                Data = new ServerTcpForwardRegisterParamsInfo
                {
                    Web = serverTcpForwardRegisterConfig.Web,
                    Tunnel = serverTcpForwardRegisterConfig.Tunnel
                },
                Connection = registerstate.TcpConnection,
                Path = "ServerTcpForward/register"
            });
        }
        public async Task<MessageResponeInfo> UnRegister()
        {
            return await messengerSender.SendReply(new MessageRequestParamsInfo<ServerTcpForwardRegisterParamsInfo>
            {
                Data = new ServerTcpForwardRegisterParamsInfo
                {
                    Web = serverTcpForwardRegisterConfig.Web,
                    Tunnel = serverTcpForwardRegisterConfig.Tunnel
                },
                Connection = registerstate.TcpConnection,
                Path = "ServerTcpForward/unregister"
            });
        }

        public async Task Request(ServerTcpForwardInfo data)
        {
            if (data.Type == ServerTcpForwardTypes.CLOSE)
            {
                ClientModel.Remove(data.RequestId);
                return;
            }

            if (!serverTcpForwardRegisterConfig.Enable)
            {
                await messengerSender.SendOnly(new MessageRequestParamsInfo<ServerTcpForwardInfo>
                {
                    Data = new ServerTcpForwardInfo
                    {
                        RequestId = data.RequestId,
                        Type = ServerTcpForwardTypes.FAIL,
                        Buffer = "客户端未开启插件".GetBytes(),
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
                        SourceSocket = ((TcpConnection)registerstate.TcpConnection).TcpSocket
                    };

                    IPEndPoint dnsEndPoint = new(NetworkHelper.GetDomainIp(data.TargetIp), data.TargetPort);
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
                await messengerSender.SendOnly(new MessageRequestParamsInfo<ServerTcpForwardInfo>
                {
                    Data = new ServerTcpForwardInfo
                    {
                        RequestId = data.RequestId,
                        Type = ServerTcpForwardTypes.FAIL,
                        Buffer = ex.ToString().GetBytes(),
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
            await messengerSender.SendOnly(new MessageRequestParamsInfo<ServerTcpForwardInfo>
            {
                Data = new ServerTcpForwardInfo
                {
                    RequestId = client.RequestId,
                    Buffer = data,
                    Type = ServerTcpForwardTypes.RESPONSE,
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