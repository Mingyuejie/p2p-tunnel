using client.servers.clientServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ddns.client
{
    public class DdnsPlugin : IClientServicePlugin
    {
    }

    public class DdnsSettingPlugin : IClientServiceSettingPlugin
    {
        public string Name => "动态域名解析";

        public string Author => "snltty";

        public string Desc => "ip变化后自动解析域名";

        public bool Enable => false;

        public object LoadSetting()
        {
            return new { };
        }

        public string SaveSetting(string jsonStr)
        {
            return string.Empty;
        }

        public bool SwitchEnable(bool enable)
        {
            return true;
        }
    }
}
