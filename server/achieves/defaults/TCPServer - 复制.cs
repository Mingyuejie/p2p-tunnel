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
    //public class TCPServer : ITcpServer
    //{
    //    private readonly ConcurrentDictionary<int, ServerModel> servers = new ConcurrentDictionary<int, ServerModel>();
    //    public SimplePushSubHandler<ServerDataWrap<TcpPacket[]>> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap<TcpPacket[]>>();
    //    private CancellationTokenSource cancellationTokenSource;

    //    private bool Running
    //    {
    //        get
    //        {
    //            return cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested;
    //        }
    //    }
    //    public void Start(int port, IPAddress ip = null)
    //    {
    //        if (Running)
    //        {
    //            return;
    //        }
    //        cancellationTokenSource = new CancellationTokenSource();
    //        BindAccept(port, ip ?? IPAddress.Any);
    //    }
    //    public void BindAccept(int port, IPAddress ip)
    //    {
    //        if (servers.ContainsKey(port)) return;

    //        Socket socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
    //        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
    //        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    //        socket.Bind(new IPEndPoint(ip, port));
    //        socket.Listen(int.MaxValue);

    //        ServerModel server = new()
    //        {
    //            Socket = socket
    //        };
    //        servers.TryAdd(port, server);
    //        server.Socket.BeginAccept(new AsyncCallback(Accept), server);
    //    }
    //    private void Accept(IAsyncResult result)
    //    {
    //        ServerModel server = (ServerModel)result.AsyncState;
    //        try
    //        {
    //            if (server.Socket != null)
    //            {
    //                Socket client = server.Socket.EndAccept(result);
    //                BindReceive(client);
    //                server.Socket.BeginAccept(new AsyncCallback(Accept), server);
    //            }
    //            else
    //            {
    //                Stop();
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            Stop();
    //            Logger.Instance.DebugError(ex);
    //        }
    //    }
    //    public void BindReceive(Socket socket, Action<SocketError> errorCallback = null)
    //    {
    //        ReceiveModel model = new ReceiveModel
    //        {
    //            ErrorCallback = errorCallback,
    //            Connection = CreateConnection(socket),
    //            Buffer = new byte[8 * 1024]
    //        };

    //        Task.Factory.FromAsync(socket.BeginReceive, socket.EndReceive,new TaskCreationOptions { });

    //        IAsyncResult res = socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, Receive, model);
    //    }

    //    private void Receive(IAsyncResult result)
    //    {
    //        ReceiveModel model = (ReceiveModel)result.AsyncState;
    //        try
    //        {
    //            if (model.Connection != null && model.Connection.Connected)
    //            {
    //                int length = model.Connection.TcpSocket.EndReceive(result);
    //                if (length > 0)
    //                {
    //                    if (model.Buffer.Length == length)
    //                    {
    //                        model.CacheBuffer.AddRange(model.Buffer);
    //                    }
    //                    else
    //                    {
    //                        model.CacheBuffer.AddRange(model.Buffer.AsSpan().Slice(0, length).ToArray());
    //                    }

    //                    if (model.Connection.TcpSocket.Available > 0)
    //                    {
    //                        byte[] bytes = new byte[model.Connection.TcpSocket.Available];
    //                        model.Connection.TcpSocket.Receive(bytes);
    //                        model.CacheBuffer.AddRange(bytes);
    //                    }
    //                    TcpPacket[] bytesArray = TcpPacket.FromArray(model.CacheBuffer).ToArray();
    //                    Receive(model, bytesArray);

    //                    if (model.Connection != null && model.Connection.Connected)
    //                    {
    //                        IAsyncResult res = model.Connection.TcpSocket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, Receive, model);
    //                    }
    //                    else
    //                    {
    //                        model.Clear();
    //                    }
    //                }
    //                else
    //                {
    //                    model.Clear();
    //                }
    //            }
    //            else
    //            {
    //                model.Clear();
    //            }
    //        }
    //        catch (SocketException ex)
    //        {
    //            Logger.Instance.DebugError(ex);
    //            model.ErrorCallback?.Invoke(ex.SocketErrorCode);
    //            model.Clear();
    //        }
    //        catch (Exception ex)
    //        {
    //            Logger.Instance.DebugError(ex);
    //            model.Clear();
    //        }
    //    }
    //    private void Receive(ReceiveModel model, TcpPacket[] bytesArray)
    //    {
    //        if (bytesArray.Length > 0)
    //        {
    //            OnPacket.Push(new ServerDataWrap<TcpPacket[]>
    //            {
    //                Data = bytesArray,
    //                Connection = model.Connection
    //            });
    //        }
    //    }

    //    public void Stop()
    //    {
    //        cancellationTokenSource?.Cancel();

    //        foreach (ServerModel server in servers.Values)
    //        {
    //            if (server != null && server.Socket != null)
    //            {
    //                server.Socket.SafeClose();
    //            }
    //        }
    //        servers.Clear();
    //    }

    //    public IConnection CreateConnection(Socket socket)
    //    {
    //        return new Connection
    //        {
    //            ServerType = ServerType.TCP,
    //            TcpSocket = socket,
    //            TcpAddress64 = (socket.RemoteEndPoint as IPEndPoint).ToInt64(),
    //            TcpAddress = (socket.RemoteEndPoint as IPEndPoint)
    //        };
    //    }
    //}

    //public class ReceiveModel
    //{
    //    public IConnection Connection { get; set; }
    //    public byte[] Buffer { get; set; }
    //    public List<byte> CacheBuffer { get; set; } = new List<byte>();

    //    public Action<SocketError> ErrorCallback { get; set; }

    //    public void Clear()
    //    {
    //        CacheBuffer.Clear();
    //        Buffer = null;
    //        ErrorCallback = null;
    //        if (Connection != null)
    //        {
    //            Connection.Disponse();
    //            Connection = null;
    //        }
    //    }
    //}

    //public class ServerModel
    //{
    //    public Socket Socket { get; set; }
    //}
}
