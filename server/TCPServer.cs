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

namespace server
{
    public class TCPServer : ITcpServer
    {
        private long Id = 0;

        private readonly ConcurrentDictionary<int, ServerModel> servers = new ConcurrentDictionary<int, ServerModel>();

        private CancellationTokenSource cancellationTokenSource;

        private bool Running
        {
            get
            {
                return cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested;
            }
        }

        public TCPServer()
        {
        }

        public void Start(int port, IPAddress ip = null)
        {
            if (Running)
            {
                return;
            }
            cancellationTokenSource = new CancellationTokenSource();
            BindAccept(port, ip ?? IPAddress.Any, cancellationTokenSource);

        }
        public void BindAccept(int port, IPAddress ip, CancellationTokenSource tokenSource)
        {
            if (servers.ContainsKey(port)) return;

            var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(ip, port));
            socket.Listen(int.MaxValue);

            ServerModel server = new()
            {
                AcceptDone = new ManualResetEvent(false),
                Socket = socket
            };
            servers.TryAdd(port, server);

            _ = Task.Factory.StartNew((e) =>
            {
                while (!tokenSource.IsCancellationRequested)
                {
                    server.AcceptDone.Reset();
                    try
                    {
                        _ = socket.BeginAccept(new AsyncCallback(Accept), server);
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Debug(ex + "");
                        Stop();
                        break;
                    }
                    _ = server.AcceptDone.WaitOne();
                }

            }, TaskCreationOptions.LongRunning, tokenSource.Token);
        }
        public void Stop()
        {
            cancellationTokenSource.Cancel();
            ReceiveModel.ClearAll();
            foreach (ServerModel server in servers.Values)
            {
                if (server != null && server.Socket != null)
                {
                    server.AcceptDone.Reset();
                    server.AcceptDone.Dispose();
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
                    Logger.Instance.Debug(ex + "");
                }
            }
            return false;
        }

        private void Accept(IAsyncResult result)
        {
            ServerModel server = (ServerModel)result.AsyncState;
            _ = server.AcceptDone.Set();

            try
            {
                Socket client = server.Socket.EndAccept(result);
                BindReceive(client);
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex + "");
            }
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
                            if (length == model.Buffer.Length)
                            {
                                Receive(model, model.Buffer);
                            }
                            else
                            {
                                byte[] bytes = new byte[length];
                                Array.Copy(model.Buffer, 0, bytes, 0, bytes.Length);
                                Receive(model, bytes);
                            }
                            _ = model.Socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
                        }
                        else
                        {
                            Logger.Instance.Debug("!Running");
                        }
                    }
                    else
                    {
                        Logger.Instance.Debug($"length:{length}");
                        model.Clear();
                    }
                }

            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionAborted)
                {
                }
                model.ErrorCallback?.Invoke(ex.SocketErrorCode);
                Logger.Instance.Debug(ex + "");
                model.Clear();
            }
            catch (Exception ex)
            {
                Logger.Instance.Debug(ex + "");
                model.Clear();
            }
        }
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null)
        {
            IPEndPoint ip = IPEndPoint.Parse(socket.RemoteEndPoint.ToString());
            Interlocked.Increment(ref Id);
            ReceiveModel model = new ReceiveModel { ErrorCallback = errorCallback, Id = Id, Address = ip, Socket = socket, Buffer = new byte[0] };
            _ = ReceiveModel.Add(model);

            model.Buffer = new byte[1024];
            _ = socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
        }
        private void Receive(ReceiveModel model, byte[] buffer)
        {
            MyStopwatch watch = new MyStopwatch();
            watch.Start();

            IPEndPoint address = IPEndPoint.Parse(model.Socket.RemoteEndPoint.ToString());
            lock (model.CacheBuffer)
            {
                model.CacheBuffer.AddRange(buffer);
            }
            action?.Invoke(new ServerDataWrap<List<byte>>
            {
                Data = model.CacheBuffer,
                Address = address,
                ServerType = ServerType.TCP,
                Socket = model.Socket
            });

            watch.Stop();
            watch.Output($"TCP 包处理时间：");
        }

        private Action<ServerDataWrap<List<byte>>> action;
        public void OnPacket(Action<ServerDataWrap<List<byte>>> action)
        {
            this.action = action;
        }
    }

    public class ReceiveModel
    {
        public static ConcurrentDictionary<long, ReceiveModel> clients = new ConcurrentDictionary<long, ReceiveModel>();

        public long Id { get; set; }
        public long ConnectId { get; set; }
        public Socket Socket { get; set; }
        public IPEndPoint Address { get; set; }
        public byte[] Buffer { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();

        public Action<SocketError> ErrorCallback { get; set; }

        public void Clear()
        {
            clients.TryRemove(Id, out _);

            Id = 0;
            ConnectId = 0;
            Address = null;
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
            return clients.TryAdd(model.Id, model);
        }
    }

    public class ServerModel
    {
        public Socket Socket { get; set; }
        public ManualResetEvent AcceptDone { get; set; }
    }

}
