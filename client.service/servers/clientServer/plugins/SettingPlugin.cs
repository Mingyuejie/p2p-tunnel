using client.servers.clientServer;
using common.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.servers.clientServer.plugins
{
    public class SettingPlugin : IClientServicePlugin
    {
        private readonly IClientServer clientServer;
        public SettingPlugin(IClientServer clientServer)
        {
            this.clientServer = clientServer;
        }

        public IEnumerable<SettingPluginInfo> List(ClientServicePluginExcuteWrap arg)
        {
            return clientServer.GetSettingPlugins();
        }

        public object Load(ClientServicePluginExcuteWrap arg)
        {
            int code = int.Parse(arg.Content);
            var plugin = clientServer.GetSettingPlugin(code);
            if (plugin != null)
            {
                return plugin.LoadSetting();
            }
            return new { };
        }

        public void Save(ClientServicePluginExcuteWrap arg)
        {
            SaveParam model = arg.Content.DeJson<SaveParam>();
            var plugin = clientServer.GetSettingPlugin(model.Code);
            if (plugin != null)
            {
                plugin.SaveSetting(arg.Content);
            }
        }
    }

    public class SaveParam
    {
        public int Code { get; set; }
        public string Content { get; set; }
    }
}
