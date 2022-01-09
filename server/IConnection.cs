using common;
using common.extends;
using server.model;
using server.packet;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public interface IConnection
    {
        public ulong ConnectId { get; set; }
        public bool Connected { get; }

        public UdpClient UdpcRecv { get; set; }
        public IPEndPoint UdpAddress { get; set; }
        public long UdpAddress64 { get; set; }
        public long UdpLastTime { get; set; }

        public Socket TcpSocket { get; set; }
        public IPEndPoint TcpAddress { get; set; }
        public long TcpAddress64 { get; set; }

        public ServerType ServerType { get; set; }

        public Task<bool> Send(byte[] data);

        public void UpdateTime(long time);
        public bool IsTimeout(long time);
        public bool IsNeedHeart(long time);

        public void Disponse();
    }


    public class Connection : IConnection
    {
        public ulong ConnectId { get; set; } = 0;
        public bool Connected
        {
            get
            {
                if (ServerType == ServerType.UDP)
                {
                    return UdpcRecv != null && UdpAddress != null;
                }
                return TcpSocket != null && TcpSocket.Connected;
            }
        }

        public UdpClient UdpcRecv { get; set; }
        public IPEndPoint UdpAddress { get; set; }
        public long UdpAddress64 { get; set; }
        public long UdpLastTime { get; set; } = Helper.GetTimeStamp();

        public Socket TcpSocket { get; set; }
        public IPEndPoint TcpAddress { get; set; }
        public long TcpAddress64 { get; set; }

        public ServerType ServerType { get; set; }

        public async Task<bool> Send(byte[] data)
        {
            if (ServerType == ServerType.TCP)
            {
                return await SendTcp(data);
            }
            return await SendUdp(data);
        }
        private async Task<bool> SendTcp(byte[] data)
        {
            if (Connected)
            {
                try
                {
                    await TcpSocket.SendAsync(TcpPacket.ToArray(data), SocketFlags.None);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            }
            return false;
        }

        private async Task<bool> SendUdp(byte[] data)
        {
            if (Connected)
            {
                try
                {
                    IEnumerable<UdpPacket> udpPackets = UdpPacket.Split(data);
                    foreach (UdpPacket udpPacket in udpPackets)
                    {
                        byte[] udpPacketDatagram = udpPacket.ToArray();
                        await UdpcRecv.SendAsync(udpPacketDatagram, udpPacketDatagram.Length, UdpAddress);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.Instance.DebugError(ex);
                }
            }
            return false;
        }

        public void UpdateTime(long time)
        {
            if (ServerType == ServerType.UDP)
            {
                UdpLastTime = time;
            }
        }

        public bool IsTimeout(long time)
        {
            return (UdpLastTime > 0 && time - UdpLastTime > 20000);
        }
        public bool IsNeedHeart(long time)
        {
            return (UdpLastTime == 0 || time - UdpLastTime > 5000);
        }

        public void Disponse()
        {
            if (TcpSocket != null)
            {
                TcpSocket.SafeClose();
                TcpSocket.Dispose();
            }
            TcpAddress = null;
            TcpAddress64 = 0;

            UdpAddress = null;
            UdpAddress64 = 0;
            UdpcRecv = null;
            UdpLastTime = 0;
        }

        class SendState
        {
            public TaskCompletionSource<bool> Tcs { get; set; }
            public Socket Socket { get; set; }
        }
    }
}
