using client.service.serverPlugins.clients;
using common;
using common.extends;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverPlugins.forward.tcp
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
            TcpForwardEventHandles.Instance.OnTcpForwardMessageHandler += OnTcpForwardMessageHandler;

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
        }

        private void OnRequest(object sender, TcpForwardRequestModel arg)
        {
            if (arg.Socket != null)
            {
                TcpForwardEventHandles.Instance.OnSendTcpForwardMessage(new OnSendTcpForwardMessageEventArg
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
        private void OnTcpForwardMessageHandler(object sender, OnTcpForwardMessageEventArg arg)
        {
            OnTcpForwardMessageEventArg _arg = arg;
            switch (_arg.Data.Type)
            {
                //A收到了B的回复
                case TcpForwardType.RESPONSE:
                    TcpForwardServer.Instance.Response(_arg.Data);
                    break;
                case TcpForwardType.RESPONSE_END:
                    TcpForwardServer.Instance.ResponseEnd(_arg.Data);
                    break;
                //A收到B的错误回复
                case TcpForwardType.FAIL:
                    TcpForwardServer.Instance.Fail(_arg.Data);
                    break;
                //B收到请求
                case TcpForwardType.REQUEST:
                    {
                        ClientModel.Get(_arg.Data.RequestId, out ClientModel client);
                        try
                        {
                            if (client == null)
                            {
                                Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                                client = new ClientModel
                                {
                                    RequestId = _arg.Data.RequestId,
                                    TargetSocket = socket,
                                    TargetPort = _arg.Data.TargetPort,
                                    AliveType = _arg.Data.AliveType,
                                    TargetIp = _arg.Data.TargetIp,
                                    SourceSocket = _arg.Packet.TcpSocket
                                };

                                DnsEndPoint dnsEndPoint = new(_arg.Data.TargetIp, _arg.Data.TargetPort);
                                socket.Connect(dnsEndPoint);


                                if (_arg.Data.AliveType == TcpForwardAliveTypes.ALIVE)
                                {
                                    ClientModel.Add(client);
                                    BindReceive(client);
                                }
                            }
                            if (client.TargetSocket.Connected)
                            {
                                _ = client.TargetSocket.Send(_arg.Data.Buffer);
                                if (_arg.Data.AliveType == TcpForwardAliveTypes.UNALIVE)
                                {
                                    var bytes = client.TargetSocket.ReceiveAll();
                                    //分块传输
                                    if (IsChunked(bytes))
                                    {
                                        //第一次传完
                                        if (IsEnd(bytes))
                                        {
                                            ReceiveEnd(client, bytes, _arg.Packet.TcpSocket);
                                        }
                                        else
                                        {
                                            Receive(client, bytes);
                                            while (true)
                                            {
                                                try
                                                {
                                                    var bytes1 = client.TargetSocket.ReceiveAll();
                                                    if(bytes1.Length > 0)
                                                    {
                                                        Receive(client, bytes1);
                                                        if (IsEnd(bytes1))
                                                        {
                                                            ReceiveEnd(client, bytes1, _arg.Packet.TcpSocket);
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
                                        ReceiveEnd(client, bytes, _arg.Packet.TcpSocket);
                                        client.Remove();
                                    }


                                    //Receive(client, client.TargetSocket.ReceiveAll());
                                    //client.TargetSocket.SafeClose();
                                    //_ = clients.TryRemove(_arg.Data.RequestId, out _);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ClientModel.Remove(_arg.Data.RequestId);
                            TcpForwardEventHandles.Instance.OnSendTcpForwardMessage(new OnSendTcpForwardMessageEventArg
                            {
                                Data = new TcpForwardModel
                                {
                                    RequestId = _arg.Data.RequestId,
                                    Type = TcpForwardType.FAIL,
                                    Buffer = Encoding.UTF8.GetBytes(ex + ""),
                                    AliveType = _arg.Data.AliveType
                                },
                                Socket = _arg.Packet.TcpSocket
                            });
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public void BindReceive(ClientModel client)
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
            TcpForwardEventHandles.Instance.OnSendTcpForwardMessage(new OnSendTcpForwardMessageEventArg
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

        private void ReceiveEnd(ClientModel client, byte[] data, Socket sourceSocket)
        {
            TcpForwardEventHandles.Instance.OnSendTcpForwardMessage(new OnSendTcpForwardMessageEventArg
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

        private byte[] chunkedHeaderBytes = Encoding.ASCII.GetBytes("Transfer-Encoding: chunked");
        /// <summary>
        /// 判断是否分块传输
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private bool IsChunked(byte[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == 10 && lines[i - 1] == 13 && lines.Length - i >= chunkedHeaderBytes.Length)
                {
                    if (Enumerable.SequenceEqual(lines.Skip(i + 1).Take(chunkedHeaderBytes.Length), chunkedHeaderBytes))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// \r\n\r\n结束 13 10 13 10
        /// </summary>
        /// <param name="lines"></param>
        /// <returns></returns>
        private bool IsEnd(byte[] lines)
        {
            return lines.Length >= 4 && lines[^1] == 10 && lines[^2] == 13 && lines[^3] == 10 && lines[^4] == 13;
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
                ConfigFileModel config = Helper.DeJsonSerializer<ConfigFileModel>(File.ReadAllText(configFileName));
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

                File.WriteAllText(configFileName, Helper.JsonSerializer(new ConfigFileModel
                {
                    Mappings = Mappings
                }));
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