using common;
using common.extends;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server.achieves.defaults
{
    public class TCPServer : ITcpServer
    {
        private Socket socket;
        public SimpleSubPushHandler<ServerDataWrap> OnPacket { get; } = new SimpleSubPushHandler<ServerDataWrap>();
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
                        BindReceive(client, bufferSize: bufferSize);
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

        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null, int bufferSize = 8 * 1024)
        {
            ReceiveModel model = new ReceiveModel
            {
                ErrorCallback = errorCallback,
                Connection = CreateConnection(socket),
                Buffer = new byte[bufferSize],
                Socket = socket
            };
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        int length = await model.Socket.ReceiveAsync(model.Buffer, SocketFlags.None);
                        if (length == 0)
                        {
                            model.Clear();
                            break;
                        }
                        model.CacheBuffer.AddRange(model.Buffer, length);
                        do
                        {
                            var memory = model.CacheBuffer.ArrayData;

                            int packageLen = BitConverter.ToInt32(memory, 0);
                            if (packageLen > model.CacheBuffer.Size - 4)
                            {
                                break;
                            }

                            await OnPacket.PushAsync(new ServerDataWrap
                            {
                                Connection = model.Connection,
                                Index = 4,
                                Length = packageLen + 4,
                                Data = memory
                            });

                            model.CacheBuffer.RemoveRange(0, packageLen + 4);
                        } while (model.CacheBuffer.Size > 4);
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
        public ArrayCacheBuffer CacheBuffer { get; set; } = new ArrayCacheBuffer();
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
