using common.extends;
using PacketDotNet;
using server.model;
using server.models;
using server.plugin;
using SharpPcap.LibPcap;
using SharpPcap;
using System;
using System.Net;
using common;
using server.service.plugins.register.caching;

namespace server.service.plugins
{
    public class RawPacketPlugin : IPlugin
    {
        private readonly IClientRegisterCaching clientRegisterCache;
        public RawPacketPlugin(IClientRegisterCaching clientRegisterCache)
        {
            this.clientRegisterCache = clientRegisterCache;
        }

        public bool Excute(PluginParamWrap data)
        {
            RawPacketModel model = data.Wrap.Content.DeBytes<RawPacketModel>();

            //A已注册
            RegisterCacheModel source = clientRegisterCache.Get(model.FormId);
            if (source == null) return false;
            //B已注册
            RegisterCacheModel target = clientRegisterCache.Get(model.ToId);
            if (target == null) return false;



            var packet = Packet.ParsePacket((LinkLayers)model.LinkLayerType, model.Data);
            var ethernetPacket = packet.Extract<EthernetPacket>();
            var ipPacket = ethernetPacket.Extract<IPPacket>();
            var tcpPacket = ipPacket.Extract<TcpPacket>();

            var sourceEnpoint = IPEndPoint.Parse(source.TcpSocket.RemoteEndPoint.ToString());
            var targetEnpoint = IPEndPoint.Parse(target.TcpSocket.RemoteEndPoint.ToString());

            ipPacket.SourceAddress = targetEnpoint.Address;
            ipPacket.DestinationAddress = sourceEnpoint.Address;
            ipPacket.TimeToLive = 128;
            ipPacket.UpdateCalculatedValues();

            tcpPacket.SourcePort = (ushort)targetEnpoint.Port;
            tcpPacket.DestinationPort = (ushort)sourceEnpoint.Port;

            tcpPacket.Synchronize = true;
            tcpPacket.Acknowledgment = true;
            tcpPacket.AcknowledgmentNumber = tcpPacket.SequenceNumber + 1;
            tcpPacket.SequenceNumber = (uint)new Random().Next(1, int.MaxValue);
            tcpPacket.UpdateTcpChecksum();
            tcpPacket.UpdateCalculatedValues();

            var distAddress = ethernetPacket.DestinationHardwareAddress;
            ethernetPacket.DestinationHardwareAddress = ethernetPacket.SourceHardwareAddress;
            ethernetPacket.SourceHardwareAddress = distAddress;

            ipPacket.PayloadPacket = tcpPacket;
            ethernetPacket.PayloadPacket = ipPacket;
            ethernetPacket.UpdateCalculatedValues();

            Console.WriteLine($" {model.FormId} -> {ethernetPacket}");

            foreach (var item in LibPcapLiveDeviceList.Instance)
            {
                try
                {
                    item.Open();
                    item.SendPacket(ethernetPacket);
                    Logger.Instance.Debug("已发送");
                }
                catch (Exception ex)
                {

                    Logger.Instance.Error(ex.Message);
                }
            }

            return true;
        }
    }
}
