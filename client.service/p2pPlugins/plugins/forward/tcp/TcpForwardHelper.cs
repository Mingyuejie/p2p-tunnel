using client.service.serverPlugins.clients;
using common;
using common.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.p2pPlugins.plugins.forward.tcp
{
    public class TcpForwardHelper
    {
        private static readonly Lazy<TcpForwardHelper> lazy = new(() => new TcpForwardHelper());
        public static TcpForwardHelper Instance => lazy.Value;

        public IPAddress IP { get; private set; }
        private readonly string configFileName = "config_tcp_forward.json";
        public List<TcpForwardRecordBaseModel> Mappings { get; set; } = new List<TcpForwardRecordBaseModel>();

        private TcpForwardHelper()
        {
            ReadConfig();

            //A来了请求 ，转发到B，
            TcpForwardServer.Instance.OnRequest += OnRequest;
            //B那边发生了错误，无法完成请求
            TcpForwardEventHandles.Instance.OnTcpForwardHandler += OnTcpForwardMessageHandler;

            TcpForwardServer.Instance.OnListeningChange += (sender, model) =>
            {
                TcpForwardRecordBaseModel mapping = Mappings.FirstOrDefault(c => c.SourcePort == model.SourcePort);
                if (mapping != null)
                {
                    mapping.Listening = model.Listening;
                }
            };

        }
        public void Start()
        {
            StartAll();
            Logger.Instance.Info("TCP转发服务已启动...");
        }

        private void OnRequest(object sender, TcpForwardRequestModel arg)
        {
            if (arg.Socket != null)
            {
                TcpForwardEventHandles.Instance.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = arg.Msg,
                    Socket = arg.Socket
                });
            }
            else
            {
                TcpForwardServer.Instance.Fail(arg.Msg, "未选择转发对象，或者未与转发对象建立连接");
            }
        }
        private void OnTcpForwardMessageHandler(object sender, OnTcpForwardEventArg arg)
        {
            switch (arg.Data.Type)
            {
                //A收到了B的回复
                case TcpForwardType.RESPONSE:
                    TcpForwardServer.Instance.Response(arg.Data);
                    break;
                case TcpForwardType.RESPONSE_END:
                    TcpForwardServer.Instance.ResponseEnd(arg.Data);
                    break;
                //A收到B的错误回复
                case TcpForwardType.FAIL:
                    TcpForwardServer.Instance.Fail(arg.Data);
                    break;
                //B收到请求
                case TcpForwardType.REQUEST:
                    {
                        Request(arg);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Request(OnTcpForwardEventArg arg)
        {
            ClientModel.Get(arg.Data.RequestId, out ClientModel client);
            try
            {
                if (client == null)
                {
                    Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    client = new ClientModel
                    {
                        RequestId = arg.Data.RequestId,
                        TargetSocket = socket,
                        TargetPort = arg.Data.TargetPort,
                        AliveType = arg.Data.AliveType,
                        TargetIp = arg.Data.TargetIp,
                        SourceSocket = arg.Packet.TcpSocket
                    };

                    DnsEndPoint dnsEndPoint = new(arg.Data.TargetIp, arg.Data.TargetPort);
                    socket.Connect(dnsEndPoint);


                    if (arg.Data.AliveType == TcpForwardAliveTypes.ALIVE)
                    {
                        ClientModel.Add(client);
                        BindReceive(client);
                    }
                }
                if (client.TargetSocket.Connected)
                {
                    _ = client.TargetSocket.Send(arg.Data.Buffer);
                    if (arg.Data.AliveType == TcpForwardAliveTypes.UNALIVE)
                    {
                        BindUnliveReceive(client);
                    }
                }
            }
            catch (Exception ex)
            {
                ClientModel.Remove(arg.Data.RequestId);
                TcpForwardEventHandles.Instance.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = new TcpForwardModel
                    {
                        RequestId = arg.Data.RequestId,
                        Type = TcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes(ex + ""),
                        AliveType = arg.Data.AliveType
                    },
                    Socket = arg.Packet.TcpSocket
                });
            }
        }

        private void BindUnliveReceive(ClientModel client)
        {
            var bytes = client.TargetSocket.ReceiveAll();
            //分块传输
            if (bytes.IsChunked())
            {
                //第一次传完
                if (bytes.IsChunkedEnd())
                {
                    ReceiveChunkedEnd(client, bytes, client.SourceSocket);
                    client.Remove();
                }
                else
                {
                    Receive(client, bytes);
                    while (true)
                    {
                        try
                        {
                            var bytes1 = client.TargetSocket.ReceiveAll();
                            if (bytes1.Length > 0)
                            {
                                Receive(client, bytes1);
                                if (bytes1.IsChunkedEnd())
                                {
                                    ReceiveChunkedEnd(client, bytes1, client.SourceSocket);
                                    client.Remove();
                                    break;
                                }
                                else
                                {
                                    Receive(client, bytes1);
                                }
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
                }
            }
            else
            {
                ReceiveChunkedEnd(client, bytes, client.SourceSocket);
                client.Remove();
            }
        }

        private void BindReceive(ClientModel client)
        {
            _ = Task.Factory.StartNew(() =>
            {
                while (client.TargetSocket.Connected && ClientModel.Contains(client.RequestId))
                {
                    try
                    {
                        var bytes = client.TargetSocket.ReceiveAll();
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
            }, TaskCreationOptions.LongRunning);
        }

        private void Receive(ClientModel client, byte[] data)
        {
            TcpForwardEventHandles.Instance.SendTcpForward(new SendTcpForwardEventArg
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
                Socket = client.SourceSocket
            });
        }

        private void ReceiveChunkedEnd(ClientModel client, byte[] data, Socket sourceSocket)
        {
            TcpForwardEventHandles.Instance.SendTcpForward(new SendTcpForwardEventArg
            {
                Data = new TcpForwardModel
                {
                    RequestId = client.RequestId,
                    Buffer = data,
                    Type = TcpForwardType.RESPONSE_END,
                    TargetPort = client.TargetPort,
                    AliveType = client.AliveType,
                    TargetIp = client.TargetIp
                },
                Socket = sourceSocket
            });
        }

        public string Del(int id)
        {

            TcpForwardRecordBaseModel map = Mappings.FirstOrDefault(c => c.ID == id);
            if (map != null)
            {
                Stop(map);
                _ = Mappings.Remove(map);
            }
            return SaveConfig();
        }

        public string Add(TcpForwardRecordBaseModel model, int id = 0)
        {
            TcpForwardRecordBaseModel portmap = Mappings.FirstOrDefault(c => c.SourcePort == model.SourcePort);
            if (id == 0)
            {
                if (portmap != null)
                {
                    return "已存在相同源端口的记录";
                }

                if (Helper.GetUsedPort().Contains(model.SourcePort))
                {
                    return "源端口已被其它程序占用";
                }

                int addid = Mappings.Count == 0 ? 1 : Mappings.Max(c => c.ID) + 1;
                model.ID = addid;
                Mappings.Add(model);
            }
            else
            {
                TcpForwardRecordBaseModel idmap = Mappings.FirstOrDefault(c => c.ID == id);
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
            }

            _ = SaveConfig();

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
                TcpForwardRecordModel model2 = new()
                {
                    AliveType = model.AliveType,
                    SourceIp = model.SourceIp,
                    SourcePort = model.SourcePort,
                    TargetIp = model.TargetIp,
                    TargetName = model.TargetName,
                    TargetPort = model.TargetPort
                };

                model2.Client = AppShareData.Instance.Clients.Values.FirstOrDefault(c => c.Name == model.TargetName);
                if (model2.Client == null)
                {
                    model2.Client = new ClientInfo { Name = model.TargetName };
                }
                TcpForwardServer.Instance.Start(model2);
                SaveConfig();
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
            TcpForwardServer.Instance.Stop(model.SourcePort);
            SaveConfig();
        }
        public void StopAll()
        {
            Mappings.ForEach(c =>
            {
                TcpForwardServer.Instance.Stop(c.SourcePort);
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
                    Mappings = config.Mappings;
                    Mappings.ForEach(c =>
                    {
                        c.Listening = false;
                    });
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

    public class ConfigFileModel
    {
        public ConfigFileModel() { }

        public List<TcpForwardRecordBaseModel> Mappings { get; set; } = new List<TcpForwardRecordBaseModel>();
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
        public TcpForwardAliveTypes AliveType { get; set; } = TcpForwardAliveTypes.UNALIVE;

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
                c.TargetSocket.SafeClose();
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