using client.servers.clientServer;
using client.service.ddns.platform;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
            Loop();
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
                        Domain = c.Domain,
                        Records = c.Records
                    })
                })
            });
        }
        public bool SwitchGroup(ClientServicePluginExcuteWrap arg)
        {
            SwitchGroupModel model = arg.Content.DeJson<SwitchGroupModel>();

            var group = config.Platforms.FirstOrDefault(c => c.Name == model.Platform).Groups.FirstOrDefault(c => c.Name == model.Group);
            group.AutoUpdate = model.AutoUpdate;

            config.SaveConfig();

            return true;
        }

        public bool AddDomain(ClientServicePluginExcuteWrap arg)
        {
            AddDomainModel model = arg.Content.DeJson<AddDomainModel>();

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
            SwitchDomainModel model = arg.Content.DeJson<SwitchDomainModel>();

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
            DeleteDomainModel model = arg.Content.DeJson<DeleteDomainModel>();
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
        public bool SetRecordStatus(ClientServicePluginExcuteWrap arg)
        {
            SetRecordStatusModel model = arg.Content.DeJson<SetRecordStatusModel>();
            return plugins[model.Platform].SwitchDomainRecordStatus(new SetDomainRecordStatusModel
            {
                RecordId = model.RecordId,
                Status = model.Status
            }, model.Domain);
        }
        public bool DelRecord(ClientServicePluginExcuteWrap arg)
        {
            DelRecordModel model = arg.Content.DeJson<DelRecordModel>();
            return plugins[model.Platform].DeleteDomainRecord(new DeleteDomainRecordModel
            {
                RecordId = model.RecordId,
            }, model.Domain);
        }
        public bool RemarkRecord(ClientServicePluginExcuteWrap arg)
        {
            RemarkRecordModel model = arg.Content.DeJson<RemarkRecordModel>();
            return plugins[model.Platform].UpdateDomainRecordRemark(new UpdateDomainRecordRemarkModel
            {
                RecordId = model.RecordId,
                Remark = model.Remark
            }, model.Domain);
        }
        public List<string> GetRecordTypes(ClientServicePluginExcuteWrap arg)
        {
            return typeof(RecordType).GetFields().Where(c => c.FieldType.IsEnum).Select(c => c.Name).ToList();
        }
        public IEnumerable<DescribeSupportLine> GetRecordLines(ClientServicePluginExcuteWrap arg)
        {
            GetRecordLinesModel model = arg.Content.DeJson<GetRecordLinesModel>();
            return plugins[model.Platform].DescribeSupportLines(model.Domain);
        }
        public bool AddRecord(ClientServicePluginExcuteWrap arg)
        {
            try
            {
                AddRecordModel model = arg.Content.DeJson<AddRecordModel>();
                if (string.IsNullOrWhiteSpace(model.RecordId))
                {
                    return plugins[model.Platform].AddDomainRecord(new AddDomainRecordModel
                    {
                        DomainName = model.DomainName,
                        Line = model.Line,
                        Priority = model.Priority,
                        RR = model.RR,
                        TTL = model.TTL,
                        Type = model.Type,
                        Value = model.Value
                    }, model.DomainName);
                }
                else
                {
                    return plugins[model.Platform].UpdateDomainRecord(new UpdateDomainRecordModel
                    {
                        RecordId = model.RecordId,
                        Line = model.Line,
                        Priority = model.Priority,
                        RR = model.RR,
                        TTL = model.TTL,
                        Type = model.Type,
                        Value = model.Value
                    }, model.DomainName);
                }
            }
            catch (Exception ex)
            {
                arg.SetCode(-1, ex.Message);
            }
            return false;
        }
        public bool SwitchRecord(ClientServicePluginExcuteWrap arg)
        {
            RecordSwitchModel model = arg.Content.DeJson<RecordSwitchModel>();

            var group = config.Platforms.FirstOrDefault(c => c.Name == model.Platform).Groups.FirstOrDefault(c => c.Name == model.Group);
            var domain = group.Domains.FirstOrDefault(c => c.Domain == model.Domain);
            if (model.AutoUpdate)
            {
                domain.Records.Add(model.Record);
            }
            else
            {
                domain.Records.Remove(model.Record);
            }
            domain.Records = domain.Records.Distinct().ToList();
            config.SaveConfig();
            return true;
        }


        private void Loop()
        {
            Task.Factory.StartNew(() =>
            {
                string oldIp = string.Empty;
                while (true)
                {
                    if (config.Enable)
                    {
                        using HttpClient client = new HttpClient();
                        client.DefaultRequestHeaders.Add("host", "ip.cn");
                        client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/94.0.4606.81 Safari/537.36");
                        JObject jobj = JObject.Parse(client.GetStringAsync("https://ip.cn/api/index?ip=&type=0").Result);
                        if (jobj["code"].ToString() == "0")
                        {
                            string ip = jobj["ip"].ToString();
                            if (ip != oldIp)
                            {
                                oldIp = ip;
                                UpdateRecord(ip);
                            }
                        }
                    }

                    System.Threading.Thread.Sleep(config.Interval);
                }

            }, TaskCreationOptions.LongRunning);
        }
        private void UpdateRecord(string ip)
        {
            foreach (var platform in config.Platforms)
            {
                //得存在这个平台的处理实现
                if (plugins.ContainsKey(platform.Name))
                {
                    foreach (var group in platform.Groups.Where(c => c.AutoUpdate == true))
                    {
                        foreach (var domain in group.Domains.Where(c => c.AutoUpdate == true))
                        {
                            //解析记录
                            var records = plugins[platform.Name].DescribeDomainRecords(new DescribeDomainRecordsModel
                            {
                                DomainName = domain.Domain,
                                PageSize = 500
                            }, domain.Domain).DomainRecords;

                            //解析记录不存在的记录
                            var excepts = domain.Records.Except(records.Select(c => c.RR));
                            if (excepts.Any())
                            {
                                Logger.Instance.Error($"{domain.Domain} 下不存在解析记录：{string.Join(",", excepts)}，无法更新，请先手动添加解析");
                            }

                            foreach (var record in records.Where(c => domain.Records.Contains(c.RR)))
                            {
                                try
                                {
                                    plugins[platform.Name].UpdateDomainRecord(new UpdateDomainRecordModel
                                    {
                                        Line = record.Line,
                                        Priority = record.Priority,
                                        RecordId = record.RecordId,
                                        RR = record.RR,
                                        TTL = record.TTL,
                                        Type = record.Type,
                                        Value = ip,
                                    }, domain.Domain);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Instance.Error($"更新域名解析错误:{ex.Message}");
                                }
                            }
                        }
                    }
                }
            }
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
            public List<string> Records { get; set; }
            public bool AutoUpdate { get; set; }
        }
    }
    public class SwitchGroupModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public bool AutoUpdate { get; set; }
    }
    public class AddDomainModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public string Domain { get; set; }
        public bool AutoUpdate { get; set; }
    }
    public class SwitchDomainModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public string Domain { get; set; }
        public bool AutoUpdate { get; set; }
    }
    public class DeleteDomainModel
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
    public class SetRecordStatusModel
    {
        public string Platform { get; set; }
        public string Domain { get; set; }
        public string RecordId { get; set; }
        public string Status { get; set; }
    }
    public class DelRecordModel
    {
        public string Platform { get; set; }
        public string Domain { get; set; }
        public string RecordId { get; set; }
    }
    public class RemarkRecordModel
    {
        public string Platform { get; set; }
        public string Domain { get; set; }
        public string RecordId { get; set; }
        public string Remark { get; set; }
    }
    public class GetRecordLinesModel
    {
        public string Platform { get; set; }
        public string Domain { get; set; }
    }
    public class AddRecordModel : AddDomainRecordModel
    {
        public string RecordId { get; set; }
        public string Platform { get; set; }
    }

    public class RecordSwitchModel
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public string Domain { get; set; }
        public string Record { get; set; }
        public bool AutoUpdate { get; set; }
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
