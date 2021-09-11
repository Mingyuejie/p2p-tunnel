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
    public class UDPServer : IUdpServer
    {
        public UDPServer()
        {
        }

        private UdpClient UdpcRecv { get; set; } = null;
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

        public void Send(RecvQueueModel<IModelBase> msg)
        {
            if (UdpcRecv == null)
            {
                return;
            }

            _ = Interlocked.Increment(ref sequence);
            IEnumerable<UdpPacket> udpPackets = msg.Data.ToUdpPackets(sequence);

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

                    if (Plugin.plugins.ContainsKey(packet.Type))
                    {
                        Plugin.plugins[packet.Type].Excute(model, ServerType.UDP);
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
