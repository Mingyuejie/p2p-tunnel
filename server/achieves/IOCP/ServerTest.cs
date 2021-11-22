using common;
using common.extends;
using server.model;
using server.packet;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace server.achieves.IOCP
{
    public class ServerTest : ITcpServer
    {
        private readonly ConcurrentDictionary<long, Socket> servers = new ConcurrentDictionary<long, Socket>();
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 1;    // read, write (don't alloc buffer space for accepts)
        SocketAsyncEventArgsPool m_readWritePool;
        Semaphore m_maxNumberAcceptedClients;

        public SimplePushSubHandler<ServerDataWrap<TcpPacket[]>> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap<TcpPacket[]>>();

        public ServerTest()
        {
            m_numConnections = 3000;
            m_receiveBufferSize = 1024;
            m_bufferManager = new BufferManager(m_receiveBufferSize * m_numConnections * opsToPreAlloc,
                m_receiveBufferSize);

            m_readWritePool = new SocketAsyncEventArgsPool(m_numConnections);
            m_maxNumberAcceptedClients = new Semaphore(m_numConnections, m_numConnections);

            m_bufferManager.InitBuffer();

            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += IO_Completed;
                readWriteEventArg.UserToken = new AsyncUserToken();

                m_bufferManager.SetBuffer(readWriteEventArg);

                m_readWritePool.Push(readWriteEventArg);
            }
        }

        public void Start(int port, IPAddress ip = null)
        {
            BindAccept(port, ip);
        }

        public void BindAccept(int port, IPAddress ip)
        {
            IPEndPoint localEndPoint = new IPEndPoint(ip, port);
            long id = localEndPoint.ToInt64();
            if (servers.ContainsKey(id))
            {
                return;
            }
            var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(ip, port));
            socket.Listen(int.MaxValue);

            servers.TryAdd(id, socket);
            StartAccept(socket, null);
        }
        public void StartAccept(Socket socket, SocketAsyncEventArgs acceptEventArg)
        {
            try
            {
                if (acceptEventArg == null)
                {
                    acceptEventArg = new SocketAsyncEventArgs();
                    acceptEventArg.UserToken = new AsyncUserToken
                    {
                        Socket = socket
                    };
                    acceptEventArg.Completed += IO_Accept;
                }
                else
                {
                    acceptEventArg.AcceptSocket = null;
                }

                m_maxNumberAcceptedClients.WaitOne();
                if (!socket.AcceptAsync(acceptEventArg))
                {
                    ProcessAccept(socket, acceptEventArg);
                }
            }
            catch (Exception)
            {
            }
        }
        private void IO_Accept(object sender, SocketAsyncEventArgs e)
        {
            AsyncUserToken token = ((AsyncUserToken)e.UserToken);
            ProcessAccept(token.Socket, e);
        }
        private void ProcessAccept(Socket socket, SocketAsyncEventArgs e)
        {
            _BindReceive(e.AcceptSocket, null);
            StartAccept(socket, e);
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    Logger.Instance.Error(e.LastOperation.ToString());
                    break;
            }
        }

        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null)
        {
            long id = (socket.LocalEndPoint as IPEndPoint).ToInt64();
            servers.AddOrUpdate(id, socket, (a, b) => socket);
            m_maxNumberAcceptedClients.WaitOne();

            _BindReceive(socket, errorCallback);
        }
        private void _BindReceive(Socket socket, Action<SocketError> errorCallback = null)
        {
            Task.Run(() =>
            {
                SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
                var token = ((AsyncUserToken)readEventArgs.UserToken);
                token.Socket = socket;
                token.ErrorCallback = errorCallback;
                if (!socket.ReceiveAsync(readEventArgs))
                {
                    ProcessReceive(readEventArgs);
                }
            });
        }
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                token.CacheBuffer.AddRange(e.Buffer.AsSpan().Slice(e.Offset, e.BytesTransferred).ToArray());
                if (token.Socket.Available > 0)
                {
                    var bytes = new byte[token.Socket.Available];
                    token.Socket.Receive(bytes);
                    token.CacheBuffer.AddRange(bytes);
                }

                TcpPacket[] bytesArray = TcpPacket.FromArray(token.CacheBuffer).ToArray();
                var socket = token.Socket;
                if (bytesArray.Length > 0)
                {
                    OnPacket.Push(new ServerDataWrap<TcpPacket[]>
                    {
                        Data = bytesArray,
                        Address = socket.RemoteEndPoint as IPEndPoint,
                        ServerType = ServerType.TCP,
                        Socket = socket
                    });
                }

                if (!token.Socket.ReceiveAsync(e))
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                CloseClientSocket(e);
                token.ErrorCallback?.Invoke(e.SocketError);
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {

            }
            else
            {
                CloseClientSocket(e);
            }
        }
        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            token.Socket.SafeClose();
            token.CacheBuffer.Clear();

            m_readWritePool.Push(e);
            m_maxNumberAcceptedClients.Release();
        }
        public void Stop()
        {
            foreach (var item in servers)
            {
                item.Value.SafeClose();
            }
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

    public class AsyncUserToken
    {

        public ManualResetEvent Mre { get; set; }
        public Socket Socket { get; set; }
        public string Msg { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();
        public Action<SocketError> ErrorCallback { get; set; }
    }
}
