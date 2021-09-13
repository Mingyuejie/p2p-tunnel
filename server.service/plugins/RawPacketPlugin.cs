using common.extends;
using PacketDotNet;
using server.model;
using server.models;
using server.plugin;
using SharpPcap.LibPcap;
using SharpPcap;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using common;

namespace server.service.plugins
{
    public class RawPacketPlugin : IPlugin
    {
        public MessageTypes MsgType => MessageTypes.SERVER_RAW_PACKET;

        public void Excute(PluginExcuteModel data, ServerType serverType)
        {
            RawPacketModel model = data.Packet.Chunk.DeBytes<RawPacketModel>();

            var packet = Packet.ParsePacket((LinkLayers)model.LinkLayerType, model.Data);
            var ipPacket = packet.Extract<IPPacket>();
            //交换 ip
            ipPacket.SourceAddress = ipPacket.DestinationAddress;
            ipPacket.DestinationAddress = IPEndPoint.Parse(data.TcpSocket.RemoteEndPoint.ToString()).Address;

            TcpPacket tcpPacket = ipPacket.Extract<TcpPacket>();

            //交换端口
            ushort sourcePort = tcpPacket.SourcePort;
            tcpPacket.SourcePort = tcpPacket.DestinationPort;
            tcpPacket.DestinationPort = (ushort)IPEndPoint.Parse(data.TcpSocket.RemoteEndPoint.ToString()).Port;

            tcpPacket.Synchronize = true;
            tcpPacket.Acknowledgment = true;
            tcpPacket.AcknowledgmentNumber = tcpPacket.SequenceNumber + 1;
            tcpPacket.SequenceNumber = TcpPacket.RandomPacket().SequenceNumber;


            Logger.Instance.Debug(tcpPacket.ToString());

            foreach (var item in LibPcapLiveDeviceList.Instance)
            {
                try
                {
                    item.Open();
                    item.SendPacket(tcpPacket);
                    Logger.Instance.Debug("已发送");
                }
                catch (Exception ex)
                {

                    Logger.Instance.Error(ex.Message);
                }
            }
        }
    }
}
