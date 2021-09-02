using client.service.serverPlugins.request;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.serverPlugins.test
{
    public class RequestTimeTest : IRequestExcutePlugin
    {
        public RequestExcuteTypes Type => RequestExcuteTypes.GET;

        public byte[] Excute(OnTcpRequestMsessageEventArg arg)
        {
            return Encoding.UTF8.GetBytes("1");
        }
    }
}
