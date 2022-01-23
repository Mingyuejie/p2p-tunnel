using client.messengers.register;
using common;
using common.extends;
using server;
using server.model;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.messengers.register
{
    public class DataTunnelRegister : IDataTunnelRegister
    {
        private readonly RegisterMessengerSender registerMessageHelper;
        private readonly Config config;
        private readonly RegisterStateInfo registerState;
        private readonly ITcpServer tcpServer;

        public DataTunnelRegister(RegisterMessengerSender registerMessageHelper, Config config, RegisterStateInfo registerState, ITcpServer tcpServer)
        {
            this.registerMessageHelper = registerMessageHelper;
            this.config = config;
            this.registerState = registerState;
            this.tcpServer = tcpServer;
        }

        public async Task<TunnelRegisterResultInfo> Register(string tunnelName)
        {
            if (!registerState.LocalInfo.TcpConnected)
            {
                return new TunnelRegisterResultInfo { Code = TunnelRegisterResultInfo.TunnelRegisterResultInfoCodes.UN_CONNECT };
            }

            int localPort = NetworkHelper.GetRandomPort();
            IPAddress serverAddress = NetworkHelper.GetDomainIp(config.Server.Ip);

            IPEndPoint bindEndpoint = new IPEndPoint(config.Client.BindIp, localPort);
            Socket tcpSocket = new(bindEndpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            tcpSocket.KeepAlive();
            tcpSocket.ReuseBind(bindEndpoint);
            tcpSocket.Connect(new IPEndPoint(serverAddress, config.Server.TcpPort));
            IConnection connection = tcpServer.BindReceive(tcpSocket, bufferSize: config.Client.TcpBufferSize);

            TunnelRegisterInfo info = await registerMessageHelper.TunnelInfo(connection);
            TunnelRegisterResultInfo res = await registerMessageHelper.TunnelRegister(new TunnelRegisterParams
            {
                TunnelName = tunnelName,
                LocalPort = localPort,
                Port = info.Port,
                Connection = registerState.TcpConnection
            });


            return res;
        }
    }
}
