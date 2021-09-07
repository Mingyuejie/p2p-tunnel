using common;
using common.cache;
using common.extends;
using server.model;
using server.plugin;
using server.service.model;
using System;
using System.Net;

namespace server.service.plugins
{
    /// <summary>
    /// 注册插件  添加到缓存，再发送一条注册反馈，失败或者成功
    /// </summary>
    public class RegisterPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_REGISTER;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            RegisterModel model = data.Packet.Chunk.DeBytes<RegisterModel>();

            if (serverType == ServerType.UDP)
            {
                if (ClientRegisterCache.Instance.GetBySameGroup(model.GroupId, model.Name) == null)
                {
                    RegisterCacheModel add = new()
                    {
                        Address = data.SourcePoint,
                        Name = model.Name,
                        LastTime = Helper.GetTimeStamp(),
                        TcpSocket = null,
                        TcpPort = 0,
                        OriginGroupId = model.GroupId,
                        LocalIps = model.LocalIps,
                        Mac = model.Mac,
                        LocalPort = model.LocalTcpPort

                    };
                    long id = ClientRegisterCache.Instance.Add(add, 0);

                    string origingid = add.OriginGroupId;
                    add.OriginGroupId = string.Empty;

                    UDPServer.Instance.Send(new RecvQueueModel<IModelBase>
                    {
                        Address = data.SourcePoint,
                        TcpCoket = null,
                        Data = new RegisterResultModel
                        {
                            Id = id,
                            Ip = data.SourcePoint.Address.ToString(),
                            Port = data.SourcePoint.Port,
                            TcpPort = 0,
                            GroupId = origingid,
                            Mac = add.Mac,
                            LocalTcpPort = add.LocalPort
                        }
                    });
                }
                else
                {
                    UDPServer.Instance.Send(new RecvQueueModel<IModelBase>
                    {
                        Address = data.SourcePoint,
                        TcpCoket = null,
                        Data = new RegisterResultModel
                        {
                            Code = -1,
                            Msg = "组中已存在同名客户端"
                        }
                    });
                }
            }
            else if (serverType == ServerType.TCP)
            {
                int tcpPort = 0;
                if (data.TcpSocket != null)
                {
                    tcpPort = IPEndPoint.Parse(data.TcpSocket.RemoteEndPoint.ToString()).Port;
                }

                if (ClientRegisterCache.Instance.UpdateTcpInfo(model.Id, data.TcpSocket, tcpPort, model.GroupId))
                {
                    TCPServer.Instance.Send(new RecvQueueModel<IModelBase>
                    {
                        Address = data.SourcePoint,
                        TcpCoket = data.TcpSocket,
                        Data = new RegisterResultModel
                        {
                            Id = model.Id,
                            Ip = data.SourcePoint.Address.ToString(),
                            Port = data.SourcePoint.Port,
                            TcpPort = tcpPort,
                            GroupId = model.GroupId,
                            Mac = model.Mac,
                            LocalTcpPort = model.LocalTcpPort
                        }
                    });
                }
                else
                {
                    TCPServer.Instance.Send(new RecvQueueModel<IModelBase>
                    {
                        Address = data.SourcePoint,
                        TcpCoket = data.TcpSocket,
                        Data = new RegisterResultModel
                        {
                            Code = -1,
                            Msg = "TCP注册失败"
                        }
                    });
                }
            }
        }
    }
}
