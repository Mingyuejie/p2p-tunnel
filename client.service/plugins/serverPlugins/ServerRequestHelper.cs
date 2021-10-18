using client.plugins.serverPlugins;
using client.plugins.serverPlugins.register;
using common;
using common.extends;
using server;
using server.model;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace client.service.plugins.serverPlugins
{
    public class ServerRequestHelper : IServerRequest
    {
        private readonly ServerPluginHelper serverPluginHelper;
        private readonly RegisterState registerState;

        public ServerRequestHelper(ServerPluginHelper serverPluginHelper, RegisterState registerState)
        {
            this.serverPluginHelper = serverPluginHelper;
            this.registerState = registerState;
        }

        public async Task<ServerMessageResponeWrap> SendReply<T>(SendEventArg<T> arg)
        {
            if (arg.Address != null)
            {
                return await serverPluginHelper.SendReply(new SendMessageWrap<T>
                {
                    Address = arg.Address,
                    Data = arg.Data,
                    Path = arg.Path,
                    Timeout = arg.Timeout,
                });
            }
            return await serverPluginHelper.SendReply(new SendMessageWrap<ForwardModel>
            {
                Address = registerState.UdpAddress,
                Data = new ForwardModel { Data = arg.Data.ToBytes(), Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });
        }
        public async Task<ServerMessageResponeWrap> SendReply(SendEventArg<byte[]> arg)
        {
            if (arg.Address != null)
            {
                return await serverPluginHelper.SendReply(new SendMessageWrap<byte[]>
                {
                    Address = arg.Address,
                    Data = arg.Data,
                    Path = arg.Path,
                    Timeout = arg.Timeout,
                });
            }
            return await serverPluginHelper.SendReply(new SendMessageWrap<ForwardModel>
            {
                Address = registerState.UdpAddress,
                Data = new ForwardModel { Data = arg.Data, Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });
        }
        public bool SendOnly<T>(SendEventArg<T> arg)
        {
            if (arg.Address != null)
            {
                return serverPluginHelper.SendOnly(new SendMessageWrap<T>
                {
                    Address = arg.Address,
                    Data = arg.Data,
                    Path = arg.Path,
                    Timeout = arg.Timeout,
                });
            }

            return serverPluginHelper.SendOnly(new SendMessageWrap<ForwardModel>
            {
                Address = registerState.UdpAddress,
                Data = new ForwardModel { Data = arg.Data.ToBytes(), Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });
        }
        public bool SendOnly(SendEventArg<byte[]> arg)
        {
            if (arg.Address != null)
            {
                return serverPluginHelper.SendOnly(new SendMessageWrap<byte[]>
                {
                    Address = arg.Address,
                    Data = arg.Data,
                    Path = arg.Path,
                    Timeout = arg.Timeout,
                });
            }

            return serverPluginHelper.SendOnly(new SendMessageWrap<ForwardModel>
            {
                Address = registerState.UdpAddress,
                Data = new ForwardModel { Data = arg.Data, Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });
        }

        public Task<ServerMessageResponeWrap> SendReplyTcp<T>(SendTcpEventArg<T> arg)
        {
            if (arg.Socket != null && arg.Socket.Connected)
            {
                return serverPluginHelper.SendReplyTcp(new SendMessageWrap<T>
                {
                    TcpCoket = arg.Socket,
                    Data = arg.Data,
                    Timeout = arg.Timeout,
                    Path = arg.Path
                });
            }

            return serverPluginHelper.SendReplyTcp(new SendMessageWrap<ForwardModel>
            {
                TcpCoket = registerState.TcpSocket,
                Data = new ForwardModel { Data = arg.Data.ToBytes(), Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });
        }
        public Task<ServerMessageResponeWrap> SendReplyTcp(SendTcpEventArg<byte[]> arg)
        {
            if (arg.Socket != null && arg.Socket.Connected)
            {
                return serverPluginHelper.SendReplyTcp(new SendMessageWrap<byte[]>
                {
                    TcpCoket = arg.Socket,
                    Data = arg.Data,
                    Timeout = arg.Timeout,
                    Path = arg.Path
                });
            }

            return serverPluginHelper.SendReplyTcp(new SendMessageWrap<ForwardModel>
            {
                TcpCoket = registerState.TcpSocket,
                Data = new ForwardModel { Data = arg.Data, Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });
        }
        public bool SendOnlyTcp<T>(SendTcpEventArg<T> arg)
        {
            if (arg.Socket != null && arg.Socket.Connected)
            {
                return serverPluginHelper.SendOnlyTcp(new SendMessageWrap<T>
                {
                    TcpCoket = arg.Socket,
                    Data = arg.Data,
                    Timeout = arg.Timeout,
                    Path = arg.Path
                });
            }

            return serverPluginHelper.SendOnlyTcp(new SendMessageWrap<ForwardModel>
            {
                TcpCoket = registerState.TcpSocket,
                Data = new ForwardModel { Data = arg.Data.ToBytes(), Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });

        }
        public bool SendOnlyTcp(SendTcpEventArg<byte[]> arg)
        {
            if (arg.Socket != null && arg.Socket.Connected)
            {
                return serverPluginHelper.SendOnlyTcp(new SendMessageWrap<byte[]>
                {
                    TcpCoket = arg.Socket,
                    Data = arg.Data,
                    Timeout = arg.Timeout,
                    Path = arg.Path
                });
            }
            return serverPluginHelper.SendOnlyTcp(new SendMessageWrap<ForwardModel>
            {
                TcpCoket = registerState.TcpSocket,
                Data = new ForwardModel { Data = arg.Data, Id = arg.Id, ToId = arg.ToId },
                Timeout = arg.Timeout,
                Path = "forward/excute"
            });
        }
    }
}
