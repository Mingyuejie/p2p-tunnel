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

        public SimplePushSubHandler<ServerDataWrap> OnPacket { get; }
    }

    public class ServerDataWrap
    {
        public Memory<byte> Data;
        public IConnection Connection;
    }
}
