using common;
using common.extends;
using server.model;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace server
{
    public interface IConnection
    {
        public ulong ConnectId { get; set; }
        public bool Connected { get; }

        public IPEndPoint Address { get; set; }
        public long Address64 { get; set; }
        public long LastTime { get; set; }

        public ServerType ServerType { get; }

        public MessageRequestWrap ReceiveRequestWrap { get; set; }
        public MessageResponseWrap ReceiveResponseWrap { get; set; }
        public ReceiveDataWrap ReceiveDataWrap { get; set; }

        public Task<bool> Send(ReadOnlyMemory<byte> data);

        public void UpdateTime(long time);
        public bool IsTimeout(long time);
        public bool IsNeedHeart(long time);

        public void Disponse();
    }
    public class UdpConnection : IConnection
    {
        public UdpClient UdpcRecv { get; set; }

        public ulong ConnectId { get; set; } = 0;
        public bool Connected => UdpcRecv != null && Address != null;

        public IPEndPoint Address { get; set; }
        public long Address64 { get; set; }
        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();

        public ServerType ServerType => ServerType.UDP;

        public MessageRequestWrap ReceiveRequestWrap { get; set; } = new MessageRequestWrap();
        public MessageResponseWrap ReceiveResponseWrap { get; set; } = new MessageResponseWrap();
        public ReceiveDataWrap ReceiveDataWrap { get; set; } = new ReceiveDataWrap();

        public async Task<bool> Send(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
                    await UdpcRecv.SendAsync(data, Address);
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
            LastTime = time;
        }

        public bool IsTimeout(long time)
        {
            return (LastTime > 0 && time - LastTime > 20000);
        }
        public bool IsNeedHeart(long time)
        {
            return (LastTime == 0 || time - LastTime > 5000);
        }

        public void Disponse()
        {
            Address = null;
            Address64 = 0;
            UdpcRecv = null;
            LastTime = 0;
            ReceiveRequestWrap = null;
            ReceiveResponseWrap = null;
            ReceiveDataWrap = null;
        }
    }


    public class TcpConnection : IConnection
    {
        public Socket TcpSocket { get; set; }

        public ulong ConnectId { get; set; } = 0;
        public bool Connected => TcpSocket != null && TcpSocket.Connected;

        public IPEndPoint Address { get; set; }
        public long Address64 { get; set; }
        public long LastTime { get; set; } = DateTimeHelper.GetTimeStamp();

        public ServerType ServerType => ServerType.TCP;

        public MessageRequestWrap ReceiveRequestWrap { get; set; } = new MessageRequestWrap();
        public MessageResponseWrap ReceiveResponseWrap { get; set; } = new MessageResponseWrap();
        public ReceiveDataWrap ReceiveDataWrap { get; set; } = new ReceiveDataWrap();

        public async Task<bool> Send(ReadOnlyMemory<byte> data)
        {
            if (Connected)
            {
                try
                {
                    await TcpSocket.SendAsync(data, SocketFlags.None);
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
            LastTime = time;
        }

        public bool IsTimeout(long time)
        {
            return (LastTime > 0 && time - LastTime > 20000);
        }
        public bool IsNeedHeart(long time)
        {
            return (LastTime == 0 || time - LastTime > 5000);
        }

        public void Disponse()
        {
            if (TcpSocket != null)
            {
                TcpSocket.SafeClose();
                TcpSocket.Dispose();
            }
            Address = null;
            Address64 = 0;

            LastTime = 0;
            ReceiveRequestWrap = null;
            ReceiveResponseWrap = null;
            ReceiveDataWrap = null;
        }
    }
}
