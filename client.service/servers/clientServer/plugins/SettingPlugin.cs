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

        public IEnumerable<SettingPluginInfo> List(ClientServicePluginExecuteWrap arg)
        {
            return clientServer.GetSettingPlugins();
        }

        public IEnumerable<string> Plugins(ClientServicePluginExecuteWrap arg)
        {
            return clientServer.GetPlugins();
        }

        public async Task<bool> Enable(ClientServicePluginExecuteWrap arg)
        {
            EnableParam model = arg.Content.DeJson<EnableParam>();
            var plugin = clientServer.GetSettingPlugin(model.ClassName);
            if (plugin != null)
            {
                return await plugin.SwitchEnable(model.Enable);
            }
            return false;
        }

        public async Task<object> Load(ClientServicePluginExecuteWrap arg)
        {
            SaveParam model = arg.Content.DeJson<SaveParam>();
            var plugin = clientServer.GetSettingPlugin(model.ClassName);
            if (plugin != null)
            {
                return await plugin.LoadSetting();
            }
            return new { };
        }

        public async Task Save(ClientServicePluginExecuteWrap arg)
        {
            SaveParam model = arg.Content.DeJson<SaveParam>();
            var plugin = clientServer.GetSettingPlugin(model.ClassName);
            if (plugin != null)
            {
                string msg = await plugin.SaveSetting(model.Content);
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

    public class EnableParam
    {
        public string ClassName { get; set; }
        public bool Enable { get; set; }
    }
}
