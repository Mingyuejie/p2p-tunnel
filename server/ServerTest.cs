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

namespace server
{
    public class ServerTest : ITcpServer
    {
        private readonly ConcurrentDictionary<long, Socket> servers = new ConcurrentDictionary<long, Socket>();
        private int m_numConnections;   // the maximum number of connections the sample is designed to handle simultaneously
        private int m_receiveBufferSize;// buffer size to use for each socket I/O operation
        BufferManager m_bufferManager;  // represents a large reusable set of buffers for all socket operations
        const int opsToPreAlloc = 2;    // read, write (don't alloc buffer space for accepts)
        SocketAsyncEventArgsPool m_readWritePool;
        Semaphore m_maxNumberAcceptedClients;

        public SimplePushSubHandler<ServerDataWrap<TcpPacket[]>> OnPacket { get; } = new SimplePushSubHandler<ServerDataWrap<TcpPacket[]>>();

        public ServerTest()
        {
            m_numConnections = 100000;
            m_receiveBufferSize = 9 * 1024;
            m_bufferManager = new BufferManager(m_receiveBufferSize * m_numConnections * opsToPreAlloc,
                m_receiveBufferSize);

            m_readWritePool = new SocketAsyncEventArgsPool(m_numConnections);
            m_maxNumberAcceptedClients = new Semaphore(m_numConnections, m_numConnections);

            m_bufferManager.InitBuffer();

            SocketAsyncEventArgs readWriteEventArg;

            for (int i = 0; i < m_numConnections; i++)
            {
                readWriteEventArg = new SocketAsyncEventArgs();
                readWriteEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                readWriteEventArg.UserToken = new AsyncUserToken();

                m_bufferManager.SetBuffer(readWriteEventArg);

                m_readWritePool.Push(readWriteEventArg);
            }
        }

        public void Start(int port, IPAddress ip = null)
        {
            IPEndPoint localEndPoint = new IPEndPoint(ip, port);
            long id = localEndPoint.ToInt64();
            if (servers.ContainsKey(id))
            {
                return;
            }

            m_maxNumberAcceptedClients = new Semaphore(m_numConnections, m_numConnections);
            var socket = new Socket(localEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(localEndPoint);
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
                    acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>((sender, e) =>
                    {
                        ProcessAccept(socket, e);
                    });
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
        public void BindAccept(int port, IPAddress ip)
        {
            IPEndPoint localEndPoint = new IPEndPoint(ip, port);
            long id = localEndPoint.ToInt64();
            if (servers.ContainsKey(id))
            {
                return;
            }
            Task.Run(() =>
            {
                var socket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Bind(new IPEndPoint(ip, port));
                socket.Listen(int.MaxValue);

                servers.TryAdd(localEndPoint.ToInt64(), socket);

                StartAccept(socket, null);
            });
        }
        public void BindReceive(Socket socket, Action<SocketError> errorCallback = null, long connectId = 0)
        {
            long id = (socket.LocalEndPoint as IPEndPoint).ToInt64();
            Task.Run(() =>
            {
                SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
                var token = ((AsyncUserToken)readEventArgs.UserToken);
                token.Socket = socket;
                token.ErrorCallback = errorCallback;

                servers.AddOrUpdate(id, socket, (a, b) => socket);

                m_maxNumberAcceptedClients.WaitOne();
                if (!socket.ReceiveAsync(readEventArgs))
                {
                    ProcessReceive(readEventArgs);
                }
            });
        }

        private void ProcessAccept(Socket socket, SocketAsyncEventArgs e)
        {
            SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
            ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;

            if (!e.AcceptSocket.ReceiveAsync(readEventArgs))
            {
                ProcessReceive(readEventArgs);
            }
            StartAccept(socket, e);
        }
        void IO_Completed(object sender, SocketAsyncEventArgs e)
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
        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = (AsyncUserToken)e.UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                byte[] data = new byte[e.BytesTransferred];
                Array.Copy(e.Buffer, e.Offset, data, 0, data.Length);
                lock (token.CacheBuffer)
                {
                    token.CacheBuffer.AddRange(data);
                    TcpPacket[] bytesArray = TcpPacket.FromArray(token.CacheBuffer).ToArray();
                    if (bytesArray.Length > 0)
                    {
                        OnPacket.Push(new ServerDataWrap<TcpPacket[]>
                        {
                            Data = bytesArray,
                            Address = token.Socket.RemoteEndPoint as IPEndPoint,
                            ServerType = ServerType.TCP,
                            Socket = token.Socket
                        });
                    }
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
        public Socket Socket { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();
        public Action<SocketError> ErrorCallback { get; set; }
    }
}
