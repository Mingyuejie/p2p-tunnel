using client.plugins.serverPlugins;
using client.plugins.serverPlugins.clients;
using client.plugins.serverPlugins.register;
using client.servers.clientServer;
using common.extends;
using server.plugins.register.caching;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.servers.clientServer.plugins
{
    public class TestPlugin : IClientServicePlugin
    {
        private readonly IServerRequest serverRequest;
        private readonly IClientInfoCaching clientInfoCaching;
        private readonly RegisterState registerState;


        public TestPlugin(IServerRequest serverRequest, IClientInfoCaching clientInfoCaching, RegisterState registerState)
        {
            this.serverRequest = serverRequest;
            this.clientInfoCaching = clientInfoCaching;
            this.registerState = registerState;
        }

        public TestPacketResponse Packet(ClientServicePluginExecuteWrap arg)
        {
            TestPacketParam param = arg.Content.DeJson<TestPacketParam>();

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
                    serverRequest.SendOnly(new SendEventArg<byte[]>
                    {
                        Data = bytes,
                        Path = "Test/Packet",
                        Connection = client.TcpConnection,
                        Timeout = 0
                    });
                }
                watch.Stop();

                return new TestPacketResponse
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
            return new TestPacketResponse { };
        }

    }

    public class TestPacketParam
    {
        public int Count { get; set; }
        public int KB { get; set; }
        public ulong Id { get; set; }
    }
    public class TestPacketResponse
    {
        public long Ms { get; set; } = 0;
        public float Us { get; set; } = 0;
        public long Ticks { get; set; } = 0;
    }
}
