﻿using common;
using common.extends;
using server.extends;
using server.model;
using server.packet;
using server.plugin;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public class TCPServer
    {
        private static readonly Lazy<TCPServer> lazy = new Lazy<TCPServer>(() => new TCPServer());
        public static TCPServer Instance => lazy.Value;
        private long Id = 0;

        private readonly Dictionary<MessageTypes, IPlugin[]> plugins = null;
        private TCPServer()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)))
                 .Select(c => (IPlugin)Activator.CreateInstance(c)).GroupBy(c => c.MsgType)
                 .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public ConcurrentDictionary<long, ReceiveModel> clients = new ConcurrentDictionary<long, ReceiveModel>();
        public ConcurrentDictionary<int, ServerModel> servers = new ConcurrentDictionary<int, ServerModel>();

        public bool IsStart { get; set; } = false;
        public bool IsListen { get; private set; } = false;

        public void Start(int port)
        {
            if (IsStart)
            {
                return;
            }

            IsStart = true;
            IsListen = true;
            BindAccept(port);
        }

        public void BindAccept(int port)
        {
            if (servers.ContainsKey(port)) return;

            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(int.MaxValue);

            ServerModel server = new ServerModel
            {
                AcceptDone = new ManualResetEvent(false),
                Socket = socket
            };
            servers.TryAdd(port, server);

            _ = Task.Factory.StartNew(() =>
            {
                while (IsStart)
                {
                    server.AcceptDone.Reset();
                    try
                    {
                        _ = socket.BeginAccept(new AsyncCallback(Accept), server);
                    }
                    catch (Exception)
                    {
                        Stop();
                        break;
                    }
                    _ = server.AcceptDone.WaitOne();
                }

            }, TaskCreationOptions.LongRunning);
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
            catch (Exception)
            {
            }
        }
        private void Receive(IAsyncResult result)
        {
            ReceiveModel model = (ReceiveModel)result.AsyncState;

            try
            {
                if (model.Socket.Connected)
                {
                    int length = model.Socket.EndReceive(result);
                    result.AsyncWaitHandle.Close();
                    if (length > 0)
                    {
                        if (IsStart)
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
                        }
                        _ = model.Socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
                    }
                    else
                    {
                        _ = clients.TryRemove(model.Id, out ReceiveModel client);
                    }
                }

            }
            catch (Exception)
            {
                _ = clients.TryRemove(model.Id, out ReceiveModel client);
            }
        }
        public void BindReceive(Socket socket)
        {
            IPEndPoint ip = IPEndPoint.Parse(socket.RemoteEndPoint.ToString());
            Interlocked.Increment(ref Id);
            ReceiveModel model = new ReceiveModel { Id = Id, Address = ip, Socket = socket, Buffer = new byte[0] };
            _ = clients.TryAdd(Id, model);

            model.Buffer = new byte[1024];
            _ = socket.BeginReceive(model.Buffer, 0, model.Buffer.Length, SocketFlags.None, new AsyncCallback(Receive), model);
        }
        public void Receive(ReceiveModel model, byte[] buffer)
        {
            IPEndPoint address = IPEndPoint.Parse(model.Socket.RemoteEndPoint.ToString());
            lock (model.CacheBuffer)
            {
                model.CacheBuffer.AddRange(buffer);
            }
            List<TcpPacket> bytesArray = TcpPacket.FromArray(model.CacheBuffer);
            if (bytesArray.Count > 0)
            {
                foreach (TcpPacket packet in bytesArray)
                {
                    PluginExcuteModel excute = new PluginExcuteModel
                    {
                        TcpSocket = model.Socket,
                        Packet = packet,
                        ServerType = ServerType.TCP,
                        SourcePoint = address
                    };
                    if (plugins.ContainsKey(packet.Type))
                    {
                        IPlugin[] _plugins = plugins[packet.Type];
                        for (int i = 0, length = _plugins.Length; i < length; i++)
                        {
                            _plugins[i].Excute(excute, ServerType.TCP);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            IsStart = false;
            foreach (ReceiveModel client in clients.Values)
            {

                if (client != null && client.Socket != null)
                {
                    client.Socket.SafeClose();
                }
            }
            clients.Clear();
            foreach (ServerModel server in servers.Values)
            {

                if (server != null && server.Socket != null)
                {
                    server.AcceptDone.Dispose();
                    server.Socket.SafeClose();
                }
            }
            servers.Clear();
        }

        public void Send(MessageRecvQueueModel<IMessageModelBase> msg)
        {
            if (IsStart)
            {
                TcpPacket tcpPackets = msg.Data.SerializeTcpMessage();
                if (msg.TcpCoket != null && msg.TcpCoket.Connected)
                {
                    try
                    {
                        msg.TcpCoket.SendTimeout = msg.Timeout;
                        _ = msg.TcpCoket.Send(tcpPackets.ToArray());
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Error(ex + "");
                    }
                }
            }
        }
    }

    public class ReceiveModel
    {
        public long Id { get; set; }
        public long ConnectId { get; set; }
        public Socket Socket { get; set; }
        public IPEndPoint Address { get; set; }
        public byte[] Buffer { get; set; }
        public List<byte> CacheBuffer { get; set; } = new List<byte>();
    }

    public class ServerModel
    {
        public Socket Socket { get; set; }
        public ManualResetEvent AcceptDone { get; set; }
    }
}
