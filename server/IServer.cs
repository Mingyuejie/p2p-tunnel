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

        public SimpleSubPushHandler<ServerDataWrap> OnPacket { get; }
    }

    public class ServerDataWrap
    {
        public byte[] Data { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public IConnection Connection;
    }
}
