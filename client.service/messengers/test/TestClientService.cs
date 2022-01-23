using client.messengers.clients;
using client.servers.clientServer;
using common.extends;
using server;
using System.Diagnostics;
using System.Threading.Tasks;

namespace client.service.messengers.reset
{
    public class TestClientService : IClientService
    {
        private readonly MessengerSender  messengerSender;
        private readonly IClientInfoCaching clientInfoCaching;


        public TestClientService(MessengerSender messengerSender, IClientInfoCaching clientInfoCaching)
        {
            this.messengerSender = messengerSender;
            this.clientInfoCaching = clientInfoCaching;
        }

        public async Task<TestPacketResponseInfo> Packet(ClientServiceParamsInfo arg)
        {
            TestPacketParamsInfo param = arg.Content.DeJson<TestPacketParamsInfo>();

            var bytes = new byte[param.KB * 1024];
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] = 1;
            }

            if (clientInfoCaching.Get(param.Id, out ClientInfo client))
            {
                var watch = new Stopwatch();
                watch.Start();
                for (int i = 0; i < param.Count; i++)
                {
                    await messengerSender.SendOnly(new  server.model.MessageRequestParamsInfo<byte[]>
                    {
                        Data = bytes,
                        Path = "Test/Packet",
                        Connection = client.TcpConnection,
                        Timeout = 0
                    });
                }
                watch.Stop();

                return new TestPacketResponseInfo
                {
                    Ms = watch.ElapsedMilliseconds,
                    Ticks = watch.ElapsedTicks,
                    Us = (watch.ElapsedTicks * 1000000F / Stopwatch.Frequency)
                };
            }
            else
            {
                arg.SetCode(-1, "请选择目标客户端");
            }
            return new TestPacketResponseInfo { };
        }

    }

    public class TestPacketParamsInfo
    {
        public int Count { get; set; }
        public int KB { get; set; }
        public ulong Id { get; set; }
    }
    public class TestPacketResponseInfo
    {
        public long Ms { get; set; } = 0;
        public float Us { get; set; } = 0;
        public long Ticks { get; set; } = 0;
    }
}
