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
        private readonly ConcurrentDictionary<int, ServerModel> servers = new ConcurrentDictionary<int, ServerModel>();
        public SimplePushSubHandler<ServerDataWrap<TcpPacket[]>> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap<TcpPacket[]>>();
        private CancellationTokenSource cancellationTokenSource;

        private bool Running
        {
            get
            {
                return cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested;
            }
        }
        public void Start(int port, IPAddress ip = null)
        {
            if (Running)
            {
                return;
            }
            cancellationTokenSource = new CancellationTokenSource();
            BindAccept(port, ip ?? IPAddress.Any);
        }
        public void BindAccept(int port, IPAddress ip)
        {
            if (servers.ContainsKey(port)) return;

            var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(ip, port));
            socket.Listen(int.MaxValue);

            ServerModel server = new()
            {
                Socket = socket
            };
            servers.TryAdd(port, server);
            _ = server.Socket.BeginAccept(new AsyncCallback(Accept), server);
        }
        private void Accept(IAsyncResult result)
        {
            ServerModel server = (ServerModel)result.AsyncState;
            try
            {
                Socket client = server.Socket.EndAccept(result);
                result.AsyncWaitHandle.Close();
                BindReceive(client);
                _ = server.Socket.BeginAccept(new AsyncCallback(Accept), server);
            }
            catch (Exception ex)
            {
                Stop();
                Logger.Instance.Debug(ex);
            }
        }
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null)
        {
            IPEndPoint ip = IPEndPoint.Parse(socket.RemoteEndPoint.ToString());
           
            ReceiveModel model = new ReceiveModel {  ErrorCallback = errorCallback, Socket = socket, Buffer = Array.Empty<byte>() };
            _ = ReceiveModel.Add(model);

            model.Buffer = new byte[8 * 1024];
            _ = socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
        }
        private void Receive(IAsyncResult result)
        {
            ReceiveModel model = (ReceiveModel)result.AsyncState;
            try
            {
                if (model.Socket != null && model.Socket.Connected)
                {
                    int length = model.Socket.EndReceive(result);
                    result.AsyncWaitHandle.Close();
                    if (length > 0)
                    {
                        if (Running)
                        {
                            if (model.Buffer.Length == length)
                            {
                                model.CacheBuffer.AddRange(model.Buffer);
                            }
                            else
                            {
                                model.CacheBuffer.AddRange(model.Buffer.AsSpan().Slice(0, length).ToArray());
                            }
                            TcpPacket[] bytesArray = TcpPacket.FromArray(model.CacheBuffer).ToArray();
                            Receive(model, bytesArray);

                            _ = model.Socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
                        }
                    }
                    else
                    {
                        model.Clear();
                    }
                }

            }
            catch (SocketException ex)
            {
                model.Clear();
                model.ErrorCallback?.Invoke(ex.SocketErrorCode);
                Logger.Instance.Debug(ex);
            }
            catch (Exception ex)
            {
                model.Clear();
                Logger.Instance.Debug(ex);
            }
        }
        private void Receive(ReceiveModel model, TcpPacket[] bytesArray)
        {
            if (bytesArray.Length > 0)
            {
                var address = model.Socket.RemoteEndPoint as IPEndPoint;
                OnPacket.Push(new ServerDataWrap<TcpPacket[]>
                {
                    Data = bytesArray,
                    Address = address,
                    ServerType = ServerType.TCP,
                    Socket = model.Socket
                });
            }
        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            ReceiveModel.ClearAll();
            foreach (ServerModel server in servers.Values)
            {
                if (server != null && server.Socket != null)
                {
                    server.Socket.SafeClose();
                }
            }
            servers.Clear();
        }
        public bool Send(byte[] data, Socket socket)
        {
            if (socket != null && socket.Connected)
            {
                try
                {
                    socket.Send(data);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.Error(ex);
                }
            }
            return false;
        }
    }

    public class ReceiveModel
    {
        public static long FlagId = 0;

        public static ConcurrentDictionary<long, ReceiveModel> clients = new ConcurrentDictionary<long, ReceiveModel>();

        public long Id { get; set; }
        public Socket Socket { get; set; }
        public byte[] Buffer { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();

        public Action<SocketError> ErrorCallback { get; set; }

        public void Clear()
        {
            clients.TryRemove(Id, out _);

            Id = 0;
            CacheBuffer.Clear();
            Buffer = null;
            ErrorCallback = null;

            if (Socket != null)
            {
                Socket.SafeClose();
                Socket = null;
            }
        }
        public static void ClearAll()
        {
            foreach (ReceiveModel client in clients.Values)
            {
                if (client != null)
                {
                    client.Clear();
                }
            }
        }
        public static bool Add(ReceiveModel model)
        {
            Interlocked.Increment(ref FlagId);
            model.Id = FlagId;
            return clients.TryAdd(model.Id, model);
        }
    }

    public class ServerModel
    {
        public Socket Socket { get; set; }
    }
}
