using common;
using common.extends;
using server.model;
using server.plugin;
using server.service.cache;
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
        private readonly IClientRegisterCache clientRegisterCache;
        private readonly ITcpServer tcpServer;
        private readonly IUdpServer udpServer;
        public RegisterPlugin(IClientRegisterCache clientRegisterCache, ITcpServer tcpServer, IUdpServer udpServer)
        {
            this.clientRegisterCache = clientRegisterCache;
            this.tcpServer = tcpServer;
            this.udpServer = udpServer;
        }

        public MessageTypes MsgType => MessageTypes.SERVER_REGISTER;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            RegisterModel model = data.Packet.Chunk.DeBytes<RegisterModel>();

            if (serverType == ServerType.UDP)
            {
                if (clientRegisterCache.GetBySameGroup(model.GroupId, model.Name) == null)
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
                    long id = clientRegisterCache.Add(add, 0);
                    string origingid = add.OriginGroupId;
                    add.OriginGroupId = string.Empty;

                    udpServer.Send(new RecvQueueModel<IModelBase>
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
                    tcpServer.Send(new RecvQueueModel<IModelBase>
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
                var endpoint = IPEndPoint.Parse(data.TcpSocket.RemoteEndPoint.ToString());
                var client = clientRegisterCache.Get(model.Id);

                if (endpoint.Address.Equals(client.Address.Address)&& clientRegisterCache.UpdateTcpInfo(model.Id, data.TcpSocket, endpoint.Port, model.GroupId))
                {
                    tcpServer.Send(new RecvQueueModel<IModelBase>
                    {
                        Address = data.SourcePoint,
                        TcpCoket = data.TcpSocket,
                        Data = new RegisterResultModel
                        {
                            Id = model.Id,
                            Ip = data.SourcePoint.Address.ToString(),
                            Port = data.SourcePoint.Port,
                            TcpPort = endpoint.Port,
                            GroupId = model.GroupId,
                            Mac = model.Mac,
                            LocalTcpPort = model.LocalTcpPort
                        }
                    });
                }
                else
                {
                    tcpServer.Send(new RecvQueueModel<IModelBase>
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
