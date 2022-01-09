using client.plugins.serverPlugins.clients;
using client.servers.clientServer;
using common.extends;
using server.plugins.register.caching;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace client.service.wakeup
{
    public class WakeUpPlugin : IClientServicePlugin
    {
        private readonly IClientInfoCaching clientInfoCaching;
        public WakeUpPlugin(IClientInfoCaching clientInfoCaching)
        {
            this.clientInfoCaching = clientInfoCaching;
        }


        public void WakeUp(ClientServicePluginExecuteWrap arg)
        {
            WakeUpModel model = arg.Content.DeJson<WakeUpModel>();

            if (model.ID > 0)
            {
                if (clientInfoCaching.Get(model.ID, out ClientInfo client))
                {
                    if (client != null)
                    {
                        model.Mac = client.Mac;
                        model.Ip = client.UdpConnection.UdpAddress.Address.ToString();
                        model.Port = client.UdpConnection.UdpAddress.Port;

                    }
                }
            }
            Send(model.Mac, model.Ip, model.Port);
        }


        private void Send(string mac, string ip, int port)
        {
            using UdpClient udp = new();

            var wolPacket = new byte[17 * 6];
            var ms = new MemoryStream(wolPacket, true);

            for (int i = 0; i < 6; i++)
            {
                ms.WriteByte(0xFF);
            }

            byte[] macs = GetMac(mac);
            for (int i = 0; i < 16; i++)
            {
                ms.Write(macs, 0, macs.Length);
            }

            IPEndPoint endpoint = new(IPAddress.Parse(ip), port);
            _ = udp.Send(wolPacket, wolPacket.Length, endpoint);
        }

        public byte[] GetMac(string mac)
        {
            return mac.Trim().Replace("-", ":").Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
        }

    }

    public class WakeUpModel
    {
        public ulong ID { get; set; } = 0;
        public string Ip { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
    }


}
