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

        public bool EncodeEnabled { get; }
        public ICrypto Crypto { get; }
        public void EncodeEnable(ICrypto crypto);

        public IPEndPoint Address { get; }
        public long Address64 { get; }

        public ServerType ServerType { get; }

        public MessageRequestWrap ReceiveRequestWrap { get; }
        public MessageResponseWrap ReceiveResponseWrap { get; }
        public ReceiveDataWrap ReceiveDataWrap { get; }

        public long LastTime { get; }
        public void UpdateTime(long time);
        public bool IsTimeout(long time);
        public bool IsNeedHeart(long time);

        public ValueTask<bool> Send(ReadOnlyMemory<byte> data);
        public void Disponse();
    }

    public abstract class Connection : IConnection
    {
        public ulong ConnectId { get; set; } = 0;
        public virtual bool Connected => false;
        public bool EncodeEnabled => Crypto != null;
        public ICrypto Crypto { get; private set; }
        void IConnection.EncodeEnable(ICrypto crypto)
        {
            Crypto = crypto;
        }


        public IPEndPoint Address { get; protected set; }
        public long Address64 { get; protected set; }

        public virtual ServerType ServerType => ServerType.UDP;

        public MessageRequestWrap ReceiveRequestWrap { get; private set; } = new MessageRequestWrap();
        public MessageResponseWrap ReceiveResponseWrap { get; private set; } = new MessageResponseWrap();
        public ReceiveDataWrap ReceiveDataWrap { get; private set; } = new ReceiveDataWrap();


        public long LastTime { get; private set; } = DateTimeHelper.GetTimeStamp();
        public void UpdateTime(long time) => LastTime = time;
        public bool IsTimeout(long time) => (LastTime > 0 && time - LastTime > 20000);
        public bool IsNeedHeart(long time) => (LastTime == 0 || time - LastTime > 5000);

        public abstract ValueTask<bool> Send(ReadOnlyMemory<byte> data);

        public virtual void Disponse()
        {
            Address = null;
            Address64 = 0;
            LastTime = 0;
            ReceiveRequestWrap = null;
            ReceiveResponseWrap = null;
            ReceiveDataWrap = null;
        }

    }

    public class UdpConnection : Connection
    {
        public UdpConnection(UdpClient udpcRecv, IPEndPoint address)
        {
            UdpcRecv = udpcRecv;

            Address = address;
            Address64 = address.ToInt64();
        }

        public UdpClient UdpcRecv { get; private set; }

        public override ServerType ServerType => ServerType.UDP;
        public override bool Connected => UdpcRecv != null;
        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
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
        public override void Disponse()
        {
            base.Disponse();
            UdpcRecv = null;
        }
    }
    public class TcpConnection : Connection
    {
        public TcpConnection(Socket tcpSocket)
        {
            TcpSocket = tcpSocket;

            Address64 = (TcpSocket.RemoteEndPoint as IPEndPoint).ToInt64();
            Address = (TcpSocket.RemoteEndPoint as IPEndPoint);
        }

        public Socket TcpSocket { get; private set; }
        public override bool Connected => TcpSocket != null && TcpSocket.Connected;
        public override ServerType ServerType => ServerType.TCP;
        public override async ValueTask<bool> Send(ReadOnlyMemory<byte> data)
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
        public override void Disponse()
        {
            base.Disponse();
            if (TcpSocket != null)
            {
                TcpSocket.SafeClose();
                TcpSocket.Dispose();
            }
        }
    }
}
