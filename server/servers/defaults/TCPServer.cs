﻿using common;
using common.extends;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server.servers.defaults
{
    public class TCPServer : ITcpServer
    {
        private Socket socket;
        public SimpleSubPushHandler<IConnection> OnPacket { get; } = new SimpleSubPushHandler<IConnection>();
        private CancellationTokenSource cancellationTokenSource;
        private int bufferSize = 8 * 1024;

        public void SetBufferSize(int bufferSize = 8192)
        {
            this.bufferSize = bufferSize;
        }
        public void Start(int port, IPAddress ip = null)
        {
            if (socket != null)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            BindAccept(port, ip ?? IPAddress.Any);
        }
        public void BindAccept(int port, IPAddress ip)
        {
            socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.KeepAlive();
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(ip, port));
            socket.Listen(int.MaxValue);

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Socket client = await socket.AcceptAsync(cancellationTokenSource.Token);
                        BindReceive(client, bufferSize: bufferSize);
                    }
                    catch (SocketException ex)
                    {
                        Logger.Instance.DebugError(ex);
                        Stop();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError(ex);
                        Stop();
                        break;
                    }
                }
            });
        }

        public IConnection BindReceive(Socket socket, Action<SocketError> errorCallback = null, int bufferSize = 8 * 1024)
        {
            ReceiveUserToken userToken = new ReceiveUserToken
            {
                ErrorCallback = errorCallback,
                Connection = CreateConnection(socket),
                Data = new byte[bufferSize].AsMemory(),
                Socket = socket
            };
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        int length = await userToken.Socket.ReceiveAsync(userToken.Data, SocketFlags.None);
                        if (length == 0)
                        {
                            userToken.Clear();
                            break;
                        }
                        userToken.DataBuffer.AddRange(userToken.Data, length);
                        do
                        {
                            int packageLen = userToken.DataBuffer.Data.Span.ToInt32();
                            if (packageLen > userToken.DataBuffer.Size - 4)
                            {
                                break;
                            }

                            userToken.Connection.ReceiveDataWrap.Data = userToken.DataBuffer.Data.Slice(4, packageLen);
                            await OnPacket.PushAsync(userToken.Connection);

                            userToken.DataBuffer.RemoveRange(0, packageLen + 4);
                        } while (userToken.DataBuffer.Size > 4);
                    }
                    catch (SocketException ex)
                    {
                        Logger.Instance.DebugError(ex);
                        userToken.ErrorCallback?.Invoke(ex.SocketErrorCode);
                        userToken.Clear();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError(ex);
                        userToken.Clear();
                        break;
                    }
                }
            });

            return userToken.Connection;
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            socket?.SafeClose();
            socket = null;
        }

        public IConnection CreateConnection(Socket socket)
        {
            return new TcpConnection(socket);
        }
    }

    public class ReceiveUserToken
    {
        public IConnection Connection { get; set; }
        public Memory<byte> Data { get; set; }
        public ReceiveDataBuffer DataBuffer { get; set; } = new ReceiveDataBuffer();
        public Socket Socket { get; set; }

        public Action<SocketError> ErrorCallback { get; set; }

        public void Clear()
        {
            DataBuffer.Clear();
            Data = null;
            ErrorCallback = null;
            if (Connection != null)
            {
                Connection.Disponse();
                Connection = null;
            }
            Socket?.SafeClose();
            Socket = null;
        }
    }
}
