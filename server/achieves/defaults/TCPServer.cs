using common;
using common.extends;
using server.extends;
using server.model;
using server.packet;
using System;
using System.Buffers;
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
        public SimplePushSubHandler<ServerDataWrap> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap>();
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
                        model.CacheBuffer.AddRange(model.Buffer.AsSpan().Slice(0, length).ToArray());

                        do
                        {
                            int packageLen = BitConverter.ToInt32(model.CacheBuffer.GetRange(0, 4).ToArray());
                            if (packageLen > model.CacheBuffer.Count - 4)
                            {
                                break;
                            }

                            byte[] rev = model.CacheBuffer.GetRange(4, packageLen).ToArray();

                            await OnPacket.PushAsync(new ServerDataWrap
                            {
                                Data = rev,
                                Connection = model.Connection
                            });

                            model.CacheBuffer.RemoveRange(0, packageLen + 4);
                        } while (model.CacheBuffer.Count > 4);
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

    public class ReceiveBuffer
    {
        private byte[] items { get; set; } = Array.Empty<byte>();
        private int size;
        public int Size
        {
            get
            {
                return size;
            }
            private set
            {
                if (value == 0)
                {
                    Array.Clear(items, 0, size);
                    items = Array.Empty<byte>();

                }
                else if (value > items.Length)
                {
                    byte[] newItems = new byte[value];
                    Array.Copy(items, newItems, items.Length);
                    items = newItems;
                }
            }
        }

        public byte this[int index]
        {
            get
            {
                if (index < size)
                {
                    return items[index];
                }
                throw new ArgumentOutOfRangeException();
            }
        }

        public Memory<byte> Memory
        {
            get
            {
                return items.AsMemory(0, size);
            }
        }

        public void AddRange(byte[] data)
        {
            BeResize(data.Length);
            Array.Copy(data, 0, items, size, data.Length);
            size += data.Length;
        }

        public void Clear(bool clearData = false)
        {
            size = 0;
            if (clearData)
            {
                Size = 0;
            }
        }

        private void BeResize(int length)
        {
            int _size = size + length;
            if (size + length > items.Length)
            {
                int newsize = items.Length * 2;
                if (newsize < _size)
                {
                    newsize = _size;
                }
                Size = newsize;
            }
        }
    }
}
