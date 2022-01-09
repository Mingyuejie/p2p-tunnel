using common;
using common.extends;
using server.extends;
using server.model;
using server.packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server.achieves.defaults
{
    public class TCPServer : ITcpServer
    {
        private Socket socket;
        public SimplePushSubHandler<ServerDataWrap<TcpPacket[]>> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap<TcpPacket[]>>();
        private CancellationTokenSource cancellationTokenSource;

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
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            socket.Bind(new IPEndPoint(ip, port));
            socket.Listen(int.MaxValue);

            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        Socket client = await socket.AcceptAsync();
                        BindReceive(client);
                    }
                    catch (SocketException)
                    {

                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.DebugError(ex);
                        Stop();
                        break;
                    }
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        Stop();
                        break;
                    }
                }
            }, cancellationTokenSource.Token);
        }

        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null)
        {
            ReceiveModel model = new ReceiveModel
            {
                ErrorCallback = errorCallback,
                Connection = CreateConnection(socket),
                Buffer = new byte[8 * 1024],
                Socket = socket
            };
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        int length = await model.Socket.ReceiveAsync(model.Buffer, SocketFlags.None);
                        if (length > 0)
                        {
                            model.CacheBuffer.AddRange(model.Buffer.AsSpan().Slice(0, length).ToArray());
                            TcpPacket[] bytesArray = TcpPacket.FromArray(model.CacheBuffer).ToArray();
                            await Receive(model, bytesArray);
                        }
                        else
                        {
                            model.Clear();
                            break;
                        }
                    }
                    catch (SocketException ex)
                    {
                        model.ErrorCallback?.Invoke(ex.SocketErrorCode);
                        model.Clear();
                        break;
                    }
                    catch (Exception ex)
                    {
                        model.Clear();
                        Logger.Instance.DebugError(ex);
                        break;
                    }
                    if (cancellationTokenSource.IsCancellationRequested)
                    {
                        model.Clear();
                        break;
                    }
                }
            }, cancellationTokenSource.Token);
        }

        private async Task Receive(ReceiveModel model, TcpPacket[] bytesArray)
        {
            if (bytesArray.Length > 0)
            {
                await OnPacket.PushAsync(new ServerDataWrap<TcpPacket[]>
                {
                    Data = bytesArray,
                    Connection = model.Connection
                });
            }
        }

        public void Stop()
        {
            cancellationTokenSource?.Cancel();
            socket?.SafeClose();
            socket = null;
        }

        public IConnection CreateConnection(Socket socket)
        {
            return new Connection
            {
                ServerType = ServerType.TCP,
                TcpSocket = socket,
                TcpAddress64 = (socket.RemoteEndPoint as IPEndPoint).ToInt64(),
                TcpAddress = (socket.RemoteEndPoint as IPEndPoint)
            };
        }
    }

    public class ReceiveModel
    {
        public IConnection Connection { get; set; }
        public byte[] Buffer { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();
        public Socket Socket { get; set; }

        public Action<SocketError> ErrorCallback { get; set; }

        public void Clear()
        {
            CacheBuffer.Clear();
            Buffer = null;
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

    public class ServerModel
    {
        public Socket Socket { get; set; }
    }
}
