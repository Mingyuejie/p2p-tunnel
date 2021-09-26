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

        public IEnumerable<string> Plugins(ClientServicePluginExcuteWrap arg)
        {
            return clientServer.GetPlugins();
        }

        public object Load(ClientServicePluginExcuteWrap arg)
        {
            SaveParam model = arg.Content.DeJson<SaveParam>();
            var plugin = clientServer.GetSettingPlugin(model.ClassName);
            if (plugin != null)
            {
                return plugin.LoadSetting();
            }
            return new { };
        }

        public void Save(ClientServicePluginExcuteWrap arg)
        {
            SaveParam model = arg.Content.DeJson<SaveParam>();
            var plugin = clientServer.GetSettingPlugin(model.ClassName);
            if (plugin != null)
            {
                string msg = plugin.SaveSetting(model.Content);
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    arg.SetCode(-1, msg);
                }
            }
        }
    }

    public class SaveParam
    {
        public string ClassName { get; set; }
        public string Content { get; set; }
    }
}
