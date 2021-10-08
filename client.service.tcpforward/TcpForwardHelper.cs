using client.plugins.serverPlugins.clients;
using client.service.tcpforward.client;
using common;
using common.extends;
using server.plugins.register.caching;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardHelper
    {
        public IPAddress IP { get; private set; }
        private readonly string configFileName = "config_tcp_forward.json";
        public List<TcpForwardRecordBaseModel> Mappings { get; set; } = new List<TcpForwardRecordBaseModel>();

        private readonly TcpForwardServer tcpForwardServer;
        private readonly TcpForwardEventHandles tcpForwardEventHandles;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly TcpForwardSettingModel tcpForwardSettingModel;

        private int maxId = 0;

        public TcpForwardHelper(TcpForwardServer tcpForwardServer, TcpForwardEventHandles tcpForwardEventHandles,
            IClientInfoCaching clientInfoCaching, TcpForwardSettingModel tcpForwardSettingModel)
        {
            this.tcpForwardServer = tcpForwardServer;
            this.tcpForwardEventHandles = tcpForwardEventHandles;
            this.clientInfoCaching = clientInfoCaching;
            this.tcpForwardSettingModel = tcpForwardSettingModel;

            ReadConfig();

            //A来了请求 ，转发到B，
            tcpForwardServer.OnRequest.Sub(OnRequest);
            //B那边发生了错误，无法完成请求
            tcpForwardEventHandles.OnTcpForwardHandler.Sub(OnTcpForwardMessageHandler);

            tcpForwardServer.OnListeningChange.Sub((model) =>
            {
                TcpForwardRecordBaseModel mapping = Mappings.FirstOrDefault(c => c.SourcePort == model.SourcePort);
                if (mapping != null)
                {
                    mapping.Listening = model.Listening;
                }
            });

        }
        public void Start()
        {
            StartAll();
            Logger.Instance.Info("TCP转发服务已启动...");
        }

        private void OnRequest(TcpForwardRequestModel arg)
        {
            if (arg.Socket == null)
            {
                tcpForwardServer.Fail(arg.Msg, "未选择转发对象，或者未与转发对象建立连接");
            }
            else
            {
                tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = arg.Msg,
                    Socket = arg.Socket
                });
            }
        }
        private void OnTcpForwardMessageHandler(OnTcpForwardEventArg arg)
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
                        Request(arg);
                    }
                    break;
                default:
                    break;
            }
        }

        private void Request(OnTcpForwardEventArg arg)
        {
            if (tcpForwardSettingModel.PortWhiteList.Length > 0 && !tcpForwardSettingModel.PortWhiteList.Contains(arg.Data.TargetPort))
            {
                tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = new TcpForwardModel
                    {
                        RequestId = arg.Data.RequestId,
                        Type = TcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes("目标端口不在端口白名单中"),
                        AliveType = arg.Data.AliveType
                    },
                    Socket = arg.Packet.TcpSocket
                });
                return;
            }
            if (tcpForwardSettingModel.PortBlackList.Contains(arg.Data.TargetPort))
            {
                tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
                {
                    Data = new TcpForwardModel
                    {
                        RequestId = arg.Data.RequestId,
                        Type = TcpForwardType.FAIL,
                        Buffer = Encoding.UTF8.GetBytes("目标端口在端口黑名单中"),
                        AliveType = arg.Data.AliveType
                    },
                    Socket = arg.Packet.TcpSocket
                });
                return;
            }
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

                    IPEndPoint dnsEndPoint = new(Helper.GetDomainIp(arg.Data.TargetIp), arg.Data.TargetPort);
                    socket.Connect(dnsEndPoint);
                    client.Stream = new NetworkStream(socket, false);

                    ClientModel.Add(client);
                    BindReceive(client);
                }
                if (client.TargetSocket.Connected)
                {
                    client.Stream.Write(arg.Data.Buffer);
                    client.Stream.Flush();
                }
            }
            catch (Exception ex)
            {
                ClientModel.Remove(arg.Data.RequestId);
                tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
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

        private void BindReceive(ClientModel client)
        {
            _ = Task.Factory.StartNew(() =>
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
            }, TaskCreationOptions.LongRunning);
        }

        public void BindUnlveReceive(ClientModel client)
        {
            client.BufferSize = new byte[1024];
            client.Stream.BeginRead(client.BufferSize, 0, client.BufferSize.Length, Read, client);
        }
        private void Read(IAsyncResult result)
        {
            result.AsyncWaitHandle.Close();
            ClientModel client = (ClientModel)result.AsyncState;
            try
            {
                int count = client.Stream.EndRead(result);
                if (count == 0)
                {
                    ClientCacheModel.Remove(client.RequestId);
                }
                else
                {
                    if (count < 1024)
                    {
                        byte[] temp = new byte[count];
                        Array.Copy(client.BufferSize, 0, temp, 0, count);
                        client.BufferSize = temp;
                    }
                    Receive(client, client.BufferSize);

                    if (client.Stream.CanRead && ClientCacheModel.Contains(client.RequestId))
                    {
                        client.BufferSize = new byte[1024];
                        client.Stream.BeginRead(client.BufferSize, 0, client.BufferSize.Length, Read, client);
                    }
                    else
                    {
                        ClientCacheModel.Remove(client.RequestId);
                    }
                }
            }
            catch (Exception)
            {
                ClientCacheModel.Remove(client.RequestId);
            }
        }

        private void Receive(ClientModel client, byte[] data)
        {
            tcpForwardEventHandles.SendTcpForward(new SendTcpForwardEventArg
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
        public string DelByPort(int port)
        {
            TcpForwardRecordBaseModel map = Mappings.FirstOrDefault(c => c.SourcePort == port);
            if (map != null)
            {
                Stop(map);
                _ = Mappings.Remove(map);
            }
            return SaveConfig();
        }
        public string DelByGroup(string group)
        {
            var olds = Mappings.Where(c => c.Group == group).ToArray();
            for (int i = 0; i < olds.Length; i++)
            {
                Stop(olds[i]);
                _ = Mappings.Remove(olds[i]);
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

                //if (Helper.GetUsedPort().Contains(model.SourcePort))
                //{
                //    return "源端口已被其它程序占用";
                //}

                System.Threading.Interlocked.Increment(ref maxId);
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
                c.TargetSocket.SafeClose();
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