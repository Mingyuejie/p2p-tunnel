using common;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;

namespace server
{
    public interface IServer
    {
        public void Start(int port, IPAddress ip = null);

        public void Stop();

        public SimpleSubPushHandler<IConnection> OnPacket { get; }
    }

    public class ReceiveDataWrap
    {
        public Memory<byte> Data { get; set; }
    }
}
