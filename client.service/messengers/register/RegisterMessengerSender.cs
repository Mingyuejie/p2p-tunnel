using client.messengers.register;
using common.extends;
using server;
using server.model;
using System;
using System.Threading.Tasks;
using static server.model.RegisterResultInfo;

namespace client.service.messengers.register
{
    public class RegisterMessengerSender
    {
        private readonly MessengerSender  messengerSender;
        private readonly RegisterStateInfo registerState;

        public RegisterMessengerSender(MessengerSender messengerSender, RegisterStateInfo registerState)
        {
            this.messengerSender = messengerSender;
            this.registerState = registerState;
        }

        public async Task Exit()
        {
            MessageResponeInfo res = await messengerSender.SendReply(new  MessageRequestParamsInfo<byte[]>
            {
                Connection = registerState.TcpConnection,
                Data = Array.Empty<byte>(),
                Path = "exit/Execute"
            });
        }
        public async Task<RegisterResult> Register(RegisterParams param)
        {
            MessageResponeInfo result = await messengerSender.SendReply(new  MessageRequestParamsInfo<RegisterParamsInfo>
            {
                Connection = registerState.UdpConnection,
                Path = "register/Execute",
                Data = new RegisterParamsInfo
                {
                    Name = param.ClientName,
                    GroupId = param.GroupId,
                    LocalIps = param.LocalIps,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort,

                },
                Timeout = param.Timeout,
            });
            if (result.Code != MessageResponeCodes.OK)
            {
                return new RegisterResult { NetState = result };
            }

            RegisterResultInfo res = result.Data.DeBytes<RegisterResultInfo>();
            if (res.Code != RegisterResultInfoCodes.OK)
            {
                return new RegisterResult { NetState = result, Data = res };
            }
            MessageResponeInfo tcpResult = await messengerSender.SendReply(new  MessageRequestParamsInfo<RegisterParamsInfo>
            {
                Connection = registerState.TcpConnection,
                Path = "register/Execute",
                Data = new RegisterParamsInfo
                {
                    Id = res.Id,
                    Name = param.ClientName,
                    GroupId = res.GroupId,
                    Mac = param.Mac,
                    LocalTcpPort = param.LocalTcpPort,
                    LocalUdpPort = param.LocalUdpPort
                },
                Timeout = param.Timeout,
            });

            if (tcpResult.Code != MessageResponeCodes.OK)
            {
                return new RegisterResult { NetState = tcpResult };
            }

            RegisterResultInfo tcpRes = tcpResult.Data.DeBytes<RegisterResultInfo>();
            return new RegisterResult { NetState = tcpResult, Data = tcpRes };
        }
        public async Task<bool> Notify()
        {
            return await messengerSender.SendOnly(new  MessageRequestParamsInfo<byte[]>
            {
                Connection = registerState.TcpConnection,
                Data = Array.Empty<byte>(),
                Path = "register/notify"
            });
        }

        public async Task<TunnelRegisterInfo> TunnelInfo(IConnection connection)
        {
            MessageResponeInfo result = await messengerSender.SendReply(new  MessageRequestParamsInfo<byte[]>
            {
                Connection = connection,
                Data = Array.Empty<byte>(),
                Path = "register/TunnelInfo"
            });
            if (result.Code != MessageResponeCodes.OK)
            {
                return new TunnelRegisterInfo { Code = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.UNKNOW };
            }

            return result.Data.DeBytes<TunnelRegisterInfo>();
        }
        public async Task<TunnelRegisterResultInfo> TunnelRegister(TunnelRegisterParams param)
        {
            MessageResponeInfo result = await messengerSender.SendReply(new  MessageRequestParamsInfo<TunnelRegisterParamsInfo>
            {
                Connection = param.Connection,
                Data = new TunnelRegisterParamsInfo
                {
                    LocalPort = param.LocalPort,
                    Port = param.Port,
                    TunnelName = param.TunnelName
                },
                Path = "register/tunnel"
            });
            if (result.Code != MessageResponeCodes.OK)
            {
                return new TunnelRegisterResultInfo { Code = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.UNKNOW };
            }

            return result.Data.DeBytes<TunnelRegisterResultInfo>();
        }
    }

    public class TunnelRegisterParams
    {
        public TunnelRegisterParams() { }

        public string TunnelName { get; set; } = string.Empty;
        public int LocalPort { get; set; } = 0;
        public int Port { get; set; } = 0;
        public IConnection Connection { get; set; }
    }

    public class RegisterParams
    {
        public string GroupId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string LocalIps { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public int Timeout { get; set; } = 15 * 1000;
        public int LocalUdpPort { get; set; } = 0;
        public int LocalTcpPort { get; set; } = 0;

    }

    public class RegisterResult
    {
        public MessageResponeInfo NetState { get; set; }
        public RegisterResultInfo Data { get; set; }
    }
}
