using client.service.serverPlugins.clients;
using common;
using common.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace client.service.clientService.plugins
{
    public class WakeUpPlugin : IClientServicePlugin
    {
        private readonly ClientsHelper clientsHelper;
        public WakeUpPlugin(ClientsHelper clientsHelper)
        {
            this.clientsHelper = clientsHelper;
        }

        public void WakeUp(ClientServicePluginExcuteWrap arg)
        {
            WakeUpModel model = arg.Content.DeJson<WakeUpModel>();

            if (model.ID > 0)
            {
                if (clientsHelper.Get(model.ID, out ClientInfo client))
                {
                    if (client != null)
                    {
                        model.Mac = client.Mac;
                        model.Ip = client.Address.Address.ToString();
                        model.Port = client.Address.Port;

                    }
                }
            }
            Send(model.Mac, model.Ip, model.Port);
        }


        private void Send(string mac, string ip, int port)
        {
            using UdpClient udp = new();

            byte[] packet = new byte[6 + (16 * 6)];
            for (int i = 0; i < 6; i++)
            {
                packet[i] = 0xFF;
            }

            byte[] macs = mac.Split(':').Select(x => Convert.ToByte(x, 16)).ToArray();
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 6; j++)
                {
                    packet[6 + (i * 6) + j] = macs[j];
                }
            }
            IPEndPoint endpoint = new(IPAddress.Parse(ip), port);
            _ = udp.Send(packet, packet.Length, endpoint);
        }
    }

    public class WakeUpModel
    {
        public long ID { get; set; } = 0;
        public string Ip { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public int Port { get; set; } = 0;
    }


}
