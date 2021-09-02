using common;
using server.extends;
using server.model;
using server.packet;
using server.plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace server
{
    public sealed class UDPServer
    {
        private static readonly Lazy<UDPServer> lazy = new Lazy<UDPServer>(() => new UDPServer());
        public static UDPServer Instance { get { return lazy.Value; } }

        private readonly Dictionary<MessageTypes, IPlugin[]> plugins = null;
        private UDPServer()
        {
            plugins = AppDomain.CurrentDomain.GetAssemblies()
                 .SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPlugin)))
                 .Select(c => (IPlugin)Activator.CreateInstance(c)).GroupBy(c => c.MsgType)
                 .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public UdpClient UdpcRecv { get; set; } = null;
        IPEndPoint IpepServer { get; set; } = null;
        long sequence = 0;
        private CancellationTokenSource cancellationTokenSource;
        private bool Running
        {
            get
            {
                return cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested;
            }
        }

        public void Start(int port, IPAddress ip = null)
        {

            if (Running)
            {
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();
            IpepServer = new IPEndPoint(ip ?? IPAddress.Any, port);
            UdpcRecv = new UdpClient(IpepServer);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                const uint IOC_IN = 0x80000000;
                int IOC_VENDOR = 0x18000000;
                int SIO_UDP_CONNRESET = (int)(IOC_IN | IOC_VENDOR | 12);
                UdpcRecv.Client.IOControl(SIO_UDP_CONNRESET, new byte[] { Convert.ToByte(false) }, null);
            }

            _ = Task.Factory.StartNew((e) =>
            {
                while (Running)
                {
                    Receive();
                }
            }, TaskCreationOptions.LongRunning, cancellationTokenSource.Token);

        }

        public void Stop()
        {
            cancellationTokenSource.Cancel();
            if (UdpcRecv != null)
            {
                UdpcRecv.Close();
                UdpcRecv.Dispose();
                UdpcRecv = null;
                IpepServer = null;
            }
        }

        public void Send(MessageRecvQueueModel<IMessageModelBase> msg)
        {
            if (UdpcRecv == null)
            {
                return;
            }

            _ = Interlocked.Increment(ref sequence);
            IEnumerable<UdpPacket> udpPackets = msg.Data.SerializeMessage(sequence);

            try
            {
                foreach (UdpPacket udpPacket in udpPackets)
                {
                    byte[] udpPacketDatagram = udpPacket.ToArray();
                    _ = UdpcRecv.SendAsync(udpPacketDatagram, udpPacketDatagram.Length, msg.Address);
                }
            }
            catch (Exception)
            {
            }
        }

        private void Receive()
        {
            try
            {
                IPEndPoint ipepClient = null;
                byte[] bytRecv = UdpcRecv.Receive(ref ipepClient);

                UdpPacket packet = UdpPacket.FromArray(ipepClient, bytRecv);
                if (packet != null)
                {
                    PluginExcuteModel model = new PluginExcuteModel
                    {
                        SourcePoint = ipepClient,
                        Packet = packet,
                        ServerType = ServerType.UDP
                    };
                    if (plugins.ContainsKey(packet.Type))
                    {
                        IPlugin[] _plugins = plugins[packet.Type];
                        for (int i = 0, length = _plugins.Length; i < length; i++)
                        {
                            _plugins[i].Excute(model, ServerType.UDP);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.Info(ex + "");
            }
        }
    }
}
