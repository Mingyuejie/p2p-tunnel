using client.servers.clientServer;
using client.service.ddns.platform;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ddns.client
{
    public class DdnsPlugin : IClientServicePlugin
    {
        private readonly Config config;
        private readonly Dictionary<string, IDdnsPlatform> plugins = new();

        public DdnsPlugin(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;

            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(c => c.GetTypes())
                .Where(c => c.GetInterfaces().Contains(typeof(IDdnsPlatform)));
            foreach (var item in types)
            {
                IDdnsPlatform plugin = (IDdnsPlatform)serviceProvider.GetService(item);
                if (!plugins.ContainsKey(plugin.Platform))
                    plugins.Add(plugin.Platform, plugin);
            }
        }

        public IEnumerable<PlatformInfo> Domains(ClientServicePluginExcuteWrap arg)
        {
            return config.Platforms.Select(c => new PlatformInfo
            {
                Name = c.Name,
                Groups = c.Groups.Select(c => new PlatformInfo.GroupInfo
                {
                    AutoUpdate = c.AutoUpdate,
                    Name = c.Name,
                    Domains = c.Domains.Select(c => new PlatformInfo.DomainInfo
                    {
                        AutoUpdate = c.AutoUpdate,
                        Domain = c.Domain
                    })
                })
            });
        }
        public bool SwitchGroup(ClientServicePluginExcuteWrap arg)
        {
            GroupSwitchModel model = arg.Content.DeJson<GroupSwitchModel>();

            var group = config.Platforms.FirstOrDefault(c => c.Name == model.Platform).Groups.FirstOrDefault(c => c.Name == model.Group);
            group.AutoUpdate = model.AutoUpdate;

            config.SaveConfig();

            return true;
        }
        public bool AddDomain(ClientServicePluginExcuteWrap arg)
        {
            DomainAddModel model = arg.Content.DeJson<DomainAddModel>();

            var group = config.Platforms.FirstOrDefault(c => c.Name == model.Platform).Groups.FirstOrDefault(c => c.Name == model.Group);
            var domain = group.Domains.FirstOrDefault(c => c.Domain == model.Domain);
            if (domain != null)
            {
                arg.SetCode(-1, "域名已存在");
                return false;
            }
            group.Domains.Add(new DomainInfo
            {
                AutoUpdate = model.AutoUpdate,
                Domain = model.Domain
            });
            config.SaveConfig();
            return true;
        }
        public bool SwitchDomain(ClientServicePluginExcuteWrap arg)
        {
            DomainSwitchModel model = arg.Content.DeJson<DomainSwitchModel>();

            var group = config.Platforms.FirstOrDefault(c => c.Name == model.Platform).Groups.FirstOrDefault(c => c.Name == model.Group);
            var domain = group.Domains.FirstOrDefault(c => c.Domain == model.Domain);
            if (domain == null)
            {
                arg.SetCode(-1, "域名不存在");
                return false;
            }
            domain.AutoUpdate = model.AutoUpdate;
            config.SaveConfig();

            return true;
        }
        public bool DeleteDomain(ClientServicePluginExcuteWrap arg)
        {
            DomainDeleteModel model = arg.Content.DeJson<DomainDeleteModel>();
            var group = config.Platforms.FirstOrDefault(c => c.Name == model.Platform).Groups.FirstOrDefault(c => c.Name == model.Group);
            var domain = group.Domains.FirstOrDefault(c => c.Domain == model.Domain);
            if (domain == null)
            {
                arg.SetCode(-1, "域名不存在");
                return false;
            }
            group.Domains.Remove(domain);
            config.SaveConfig();

            return true;
        }

        public DescribeDomainRecord GetRecords(ClientServicePluginExcuteWrap arg)
        {
            GetRecordsModel model = arg.Content.DeJson<GetRecordsModel>();
            return plugins[model.Platform].DescribeDomainRecords(new DescribeDomainRecordsModel
            {
                DomainName = model.Domain,
                PageSize = 500
            }, model.Domain);
        }


    }

    public class PlatformInfo
    {
        public string Name { get; set; }
        public IEnumerable<GroupInfo> Groups { get; set; }

        public class GroupInfo
        {
            public string Name { get; set; }
            public bool AutoUpdate { get; set; }
            public IEnumerable<DomainInfo> Domains { get; set; }
        }

        public class DomainInfo
        {
            public string Domain { get; set; }
            public bool AutoUpdate { get; set; }
        }
    }
    public class GroupSwitchModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public bool AutoUpdate { get; set; }
    }
    public class DomainAddModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public string Domain { get; set; }
        public bool AutoUpdate { get; set; }
    }
    public class DomainSwitchModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public string Domain { get; set; }
        public bool AutoUpdate { get; set; }
    }
    public class DomainDeleteModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public string Domain { get; set; }
    }

    public class GetRecordsModel
    {
        public string Platform { get; set; }
        public string Domain { get; set; }
    }


    public class DdnsSettingPlugin : IClientServiceSettingPlugin
    {
        private readonly Config config;
        public DdnsSettingPlugin(Config config)
        {
            this.config = config;
        }

        public string Name => "动态域名解析";

        public string Author => "snltty";

        public string Desc => "ip变化后自动解析域名";

        public bool Enable => config.Enable;

        public object LoadSetting()
        {
            return config;
        }

        public string SaveSetting(string jsonStr)
        {
            Config _config = jsonStr.DeJson<Config>();
            config.Enable = _config.Enable;
            config.Interval = _config.Interval;
            config.Platforms = _config.Platforms;
            config.SaveConfig();
            return string.Empty;
        }

        public bool SwitchEnable(bool enable)
        {
            return true;
        }
    }
}
