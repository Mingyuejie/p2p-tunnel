using client.service.tcpforward;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace client.service.album
{
    public class AlbumPlugin
    {
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddPunchHolePlugin(this ServiceCollection obj)
        {
            return obj;
        }

        public static ServiceProvider UsePunchHolePlugin(this ServiceProvider obj)
        {
            TcpForwardHelper helper = obj.GetService<TcpForwardHelper>();
            helper.Add(new TcpForwardRecordBaseModel
            {
                AliveType = TcpForwardAliveTypes.UNALIVE,
                ID = 0,
                SourcePort = 5411,
                SourceIp = IPAddress.Any.ToString(),
                TargetPort = 5411,
                TargetIp = IPAddress.Loopback.ToString(),
                TargetName = ""
            });

            return obj;
        }
    }
}
