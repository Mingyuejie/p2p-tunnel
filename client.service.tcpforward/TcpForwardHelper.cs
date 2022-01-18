using client.plugins.serverPlugins.clients;
using client.service.tcpforward.client;
using common;
using common.extends;
using server;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardHelper
    {
        public IPAddress IP { get; private set; }
        private readonly string configFileName = "config_tcp_forward.json";
        public List<TcpForwardRecordBaseModel> Mappings { get; set; } = new List<TcpForwardRecordBaseModel>();

        private readonly ITcpForwardServer tcpForwardServer;
        private readonly TcpForwardEventHandles tcpForwardEventHandles;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly TcpForwardSettingModel tcpForwardSettingModel;

        private int maxId = 0;

        public TcpForwardHelper(ITcpForwardServer tcpForwardServer, TcpForwardEventHandles tcpForwardEventHandles,
            IClientInfoCaching clientInfoCaching, TcpForwardSettingModel tcpForwardSettingModel)
        {
            this.tcpForwardServer = tcpForwardServer;
            this.tcpForwardEventHandles = tcpForwardEventHandles;
            this.clientInfoCaching = clientInfoCaching;
            this.tcpForwardSettingModel = tcpForwardSettingModel;

            ReadConfig();

            //A来了请求 ，转发到B，
            tcpForwardServer.OnRequest.SubAsync(OnRequest);
            //B接收到A的请求
            tcpForwardEventHandles.OnTcpForwardHandler.SubAsync(OnTcpForwardMessageHandler);

            tcpForwardServer.OnListeningChange.Sub((model) =>
            {
                TcpForwardRecordBaseModel mapping = Mappings.FirstOrDefault(c => c.SourcePort == model.SourcePort);
                if (mapping != null)
                {
                    mapping.Listening = model.Listening;
                }
            });

            clientInfoCaching.OnOffline.Sub((client) =>
            {
                ClientModel.ClearFromId(client.Id);
            });

        }
        public void Start()
        {
            StartAll();
            Logger.Instance.Info("TCP转发服务已启动...");
        }

        private async Task OnRequest(TcpForwardRequestModel arg)
        {
            if (arg.Connection == null)
            {
                tcpForwardServer.Fail(arg.Msg, "未选择转发对象，或者未与转发对象建立连接");
            }
            else
            {
                await tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = arg.Msg,
                    Connection = arg.Connection
                });
            }
        }
        private async Task OnTcpForwardMessageHandler(OnTcpForwardEventArg arg)
        {
            switch (arg.Data.Type)
            {
                //A收到了B的回复
                case TcpForwardType.RESPONSE:
                    tcpForwardServer.Response(arg.Data);
                    break;
                //A收到B的错误回复
                case TcpForwardType.FAIL:
                    tcpForwardServer.Fail(arg.Data);
                    break;
                //B收到请求
                case TcpForwardType.REQUEST:
                    {
                        await Request(arg);
                    }
                    break;
                //关闭
                case TcpForwardType.CLOSE:
                    {
                        ClientModel.Remove(arg.Data.RequestId);
                    }
                    break;
                default:
                    break;
            }
        }
        private async Task Request(OnTcpForwardEventArg arg)
        {

            if (!tcpForwardSettingModel.Enable)
            {
                await tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = new TcpForwardModel
                    {
                        RequestId = arg.Data.RequestId,
                        Type = TcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes("插件未开启"),
                        AliveType = arg.Data.AliveType
                    },
                    Connection = arg.Connection
                });
                return;
            }
            if (tcpForwardSettingModel.PortWhiteList.Length > 0 && !tcpForwardSettingModel.PortWhiteList.Contains(arg.Data.TargetPort))
            {
                await tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = new TcpForwardModel
                    {
                        RequestId = arg.Data.RequestId,
                        Type = TcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes("目标端口不在端口白名单中"),
                        AliveType = arg.Data.AliveType
                    },
                    Connection = arg.Connection
                });
                return;
            }
            if (tcpForwardSettingModel.PortBlackList.Contains(arg.Data.TargetPort))
            {
                await tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = new TcpForwardModel
                    {
                        RequestId = arg.Data.RequestId,
                        Type = TcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes("目标端口在端口黑名单中"),
                        AliveType = arg.Data.AliveType
                    },
                    Connection = arg.Connection
                });
                return;
            }
            ClientModel.Get(arg.Data.RequestId, out ClientModel client);
            try
            {
                if (client == null)
                {
                    Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    client = new ClientModel
                    {
                        RequestId = arg.Data.RequestId,
                        TargetSocket = socket,
                        TargetPort = arg.Data.TargetPort,
                        AliveType = arg.Data.AliveType,
                        TargetIp = arg.Data.TargetIp,
                        SourceConnection = arg.Connection,
                        FromId = arg.Data.FromID
                    };
                    IPEndPoint dnsEndPoint = new(NetworkHelper.GetDomainIp(arg.Data.TargetIp), arg.Data.TargetPort);
                    socket.BeginConnect(dnsEndPoint, Connect, new ConnectState
                    {
                        Client = client,
                        Data = arg.Data.Buffer
                    });
                }
                else
                {
                    await client.Stream.WriteAsync(arg.Data.Buffer);
                    await client.Stream.FlushAsync();
                    if (client.AliveType == TcpForwardAliveTypes.WEB)
                    {
                        await Receive(client, client.Stream.ReceiveAll());
                    }
                }

            }
            catch (Exception ex)
            {
                ClientModel.Remove(arg.Data.RequestId);
                await tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = new TcpForwardModel
                    {
                        RequestId = arg.Data.RequestId,
                        Type = TcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes(ex + ""),
                        AliveType = arg.Data.AliveType
                    },
                    Connection = arg.Connection
                });
            }
        }
        private void Connect(IAsyncResult result)
        {
            Task.Run(async () =>
            {
                var state = (ConnectState)result.AsyncState;
                try
                {
                    state.Client.TargetSocket.EndConnect(result);
                    state.Client.Stream = new NetworkStream(state.Client.TargetSocket, false);

                    ClientModel.Add(state.Client);
                    if (state.Client.AliveType == TcpForwardAliveTypes.TUNNEL)
                    {
                        BindReceive(state.Client);
                    }
                    if (state.Client.TargetSocket.Connected)
                    {
                        await state.Client.Stream.WriteAsync(state.Data);
                        await state.Client.Stream.FlushAsync();
                        state.Data = Array.Empty<byte>();
                    }
                    if (state.Client.AliveType == TcpForwardAliveTypes.WEB)
                    {
                        await Receive(state.Client, state.Client.Stream.ReceiveAll());
                    }

                }
                catch (Exception ex)
                {
                    ClientModel.Remove(state.Client.RequestId);
                    await tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                    {
                        Data = new TcpForwardModel
                        {
                            RequestId = state.Client.RequestId,
                            Type = TcpForwardType.FAIL,
                            Buffer = Encoding.UTF8.GetBytes(ex.Message),
                            AliveType = state.Client.AliveType
                        },
                        Connection = state.Client.SourceConnection
                    });
                }
            });
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
            await tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
            {
                Data = new TcpForwardModel
                {
                    RequestId = client.RequestId,
                    Buffer = data,
                    Type = TcpForwardType.RESPONSE,
                    TargetPort = client.TargetPort,
                    AliveType = client.AliveType,
                    TargetIp = client.TargetIp
                },
                Connection = client.SourceConnection
            });
        }

        public string Del(int id)
        {
            TcpForwardRecordBaseModel map = Mappings.FirstOrDefault(c => c.ID == id);
            if (map != null)
            {
                Stop(map);
                Mappings.Remove(map);
            }
            return SaveConfig();
        }
        public string DelByPort(int port)
        {
            TcpForwardRecordBaseModel map = Mappings.FirstOrDefault(c => c.SourcePort == port);
            if (map != null)
            {
                Stop(map);
                Mappings.Remove(map);
            }
            return SaveConfig();
        }
        public string DelByGroup(string group)
        {
            var olds = Mappings.Where(c => c.Group == group).ToArray();
            for (int i = 0; i < olds.Length; i++)
            {
                Stop(olds[i]);
                Mappings.Remove(olds[i]);
            }
            return SaveConfig();
        }
        public TcpForwardRecordBaseModel GetByPort(int port)
        {
            return Mappings.FirstOrDefault(c => c.SourcePort == port);
        }

        public string Add(TcpForwardRecordBaseModel model)
        {
            TcpForwardRecordBaseModel portmap = Mappings.FirstOrDefault(c => c.SourcePort == model.SourcePort);
            if (model.ID == 0)
            {
                if (portmap != null)
                {
                    return "已存在相同源端口的记录";
                }
                Interlocked.Increment(ref maxId);
                model.ID = maxId;
                Mappings.Add(model);
            }
            else
            {
                TcpForwardRecordBaseModel idmap = Mappings.FirstOrDefault(c => c.ID == model.ID);

                if (idmap.SourcePort == model.SourcePort && idmap.ID != portmap.ID)
                {
                    return "已存在相同源端口的记录";
                }

                idmap.SourceIp = model.SourceIp;
                idmap.SourcePort = model.SourcePort;
                idmap.TargetName = model.TargetName;
                idmap.TargetPort = model.TargetPort;
                idmap.AliveType = model.AliveType;
                idmap.TargetIp = model.TargetIp;
                idmap.Desc = model.Desc;
                idmap.Editable = model.Editable;
                idmap.Group = model.Group;
            }

            SaveConfig();

            return string.Empty;
        }
        public string Start(int id)
        {
            return Start(Mappings.FirstOrDefault(c => c.ID == id));
        }
        public string Start(TcpForwardRecordBaseModel model)
        {
            try
            {
                if (model != null)
                {
                    TcpForwardRecordModel model2 = new()
                    {
                        AliveType = model.AliveType,
                        SourceIp = model.SourceIp,
                        SourcePort = model.SourcePort,
                        TargetIp = model.TargetIp,
                        TargetName = model.TargetName,
                        TargetPort = model.TargetPort,
                        Desc = model.Desc,
                        Editable = model.Editable
                    };

                    model2.Client = clientInfoCaching.All().FirstOrDefault(c => c.Name == model.TargetName);
                    if (model2.Client == null)
                    {
                        model2.Client = new ClientInfo { Name = model.TargetName };
                    }
                    tcpForwardServer.Start(model2);
                    SaveConfig();
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        public void StartAll()
        {
            StopAll();
            Mappings.ForEach(c =>
            {
                Start(c);
            });
        }
        public void Stop(int id)
        {
            Stop(Mappings.FirstOrDefault(c => c.ID == id));
        }
        public void Stop(TcpForwardRecordBaseModel model)
        {
            tcpForwardServer.Stop(model.SourcePort);
            SaveConfig();
        }
        public void StopAll()
        {
            Mappings.ForEach(c =>
            {
                tcpForwardServer.Stop(c.SourcePort);
            });
            SaveConfig();
        }

        private void ReadConfig()
        {
            if (File.Exists(configFileName))
            {
                ConfigFileModel config = File.ReadAllText(configFileName).DeJson<ConfigFileModel>();
                if (config != null)
                {
                    config.Mappings.ForEach(c =>
                    {
                        c.Listening = false;
                    });
                    Mappings = config.Mappings;

                    maxId = Mappings.Count == 0 ? 1 : Mappings.Max(c => c.ID);
                }
            }
        }
        private string SaveConfig()
        {
            try
            {

                File.WriteAllText(configFileName, new ConfigFileModel
                {
                    Mappings = Mappings
                }.ToJson());
                return string.Empty;
            }
            catch (Exception ex)
            {
                return (ex.Message);
            }
        }
    }

    public class ConnectState
    {
        public ClientModel Client { get; set; }
        public byte[] Data { get; set; }
    }

    public class ConfigFileModel
    {
        public ConfigFileModel() { }
        public List<TcpForwardRecordBaseModel> Mappings { get; set; } = new List<TcpForwardRecordBaseModel>();
    }

    public class ClientModel
    {
        public ulong RequestId { get; set; }
        public ulong FromId { get; set; }
        public int SourcePort { get; set; } = 0;
        public IConnection SourceConnection { get; set; }
        public Socket TargetSocket { get; set; }
        public byte[] BufferSize { get; set; }
        public string TargetIp { get; set; } = string.Empty;
        public int TargetPort { get; set; } = 0;
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.WEB;
        public NetworkStream Stream { get; set; }
        private readonly static ConcurrentDictionary<ulong, ClientModel> clients = new();

        public static IEnumerable<ulong> Ids()
        {
            return clients.Keys;
        }

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

        public static void ClearFromId(ulong id)
        {
            IEnumerable<ulong> requestIds = clients.Where(c => c.Value.FromId == id).Select(c => c.Key);
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

    public class ConnectPool
    {
        ConcurrentDictionary<EndPoint, ConcurrentStack<Socket>> connectStack = new ConcurrentDictionary<EndPoint, ConcurrentStack<Socket>>();

        public Socket Get(EndPoint endPoint)
        {
            bool state = connectStack.TryGetValue(endPoint, out ConcurrentStack<Socket> stack);
            if (!state)
            {
                stack = new ConcurrentStack<Socket>();
                connectStack.TryAdd(endPoint, stack);
            }
            if (!stack.TryPop(out Socket socket))
            {
                socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.Connect(endPoint);
            }
            return socket;
        }

        public void Return(Socket socket)
        {
            if (connectStack.TryGetValue(socket.RemoteEndPoint, out ConcurrentStack<Socket> stack))
            {
                stack.Push(socket);
            }
        }
    }
}