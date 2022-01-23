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
using System.Threading.Tasks;

namespace client.service.ddns
{
    public class DdnsClientService : IClientService
    {
        private readonly Config config;
        private readonly Dictionary<string, IDdnsPlatform> plugins = new();

        public DdnsClientService(Config config, ServiceProvider serviceProvider)
        {
            this.config = config;

            foreach (Type item in ReflectionHelper.GetInterfaceSchieves(AppDomain.CurrentDomain.GetAssemblies(), typeof(IDdnsPlatform)))
            {
                IDdnsPlatform plugin = (IDdnsPlatform)serviceProvider.GetService(item);
                if (!plugins.ContainsKey(plugin.Platform))
                    plugins.Add(plugin.Platform, plugin);
            }
            Loop();
        }

        public IEnumerable<PlatformResultInfo> Platforms(ClientServiceParamsInfo arg)
        {
            return config.Platforms.Select(c => new PlatformResultInfo
            {
                Name = c.Name,
                Groups = c.Groups.Select(c => new PlatformResultInfo.GroupInfo
                {
                    Name = c.Name,
                    Records = c.Records
                })
            });
        }
        public DescribeDomains Domains(ClientServiceParamsInfo arg)
        {
            ParamBasePageParamsInfo model = arg.Content.DeJson<ParamBasePageParamsInfo>();
            return plugins[model.Platform].DescribeDomains(new DescribeDomainsParamsInfo
            {
                PageSize = model.PageSize,
                PageNumber = model.PageNumber
            }, model.Group);
        }
        public bool AddDomain(ClientServiceParamsInfo arg)
        {
            try
            {
                ParamBaseInfo model = arg.Content.DeJson<ParamBaseInfo>();
                return plugins[model.Platform].AddDomain(model.Group, model.Domain);
            }
            catch (Exception ex)
            {
                arg.SetCode(-1, ex.Message);
            }
            return false;
        }
        public bool DelDomain(ClientServiceParamsInfo arg)
        {
            ParamBaseInfo model = arg.Content.DeJson<ParamBaseInfo>();
            return plugins[model.Platform].DeleteDomain(model.Group, model.Domain);
        }

        public DescribeDomainRecordsInfo GetRecords(ClientServiceParamsInfo arg)
        {
            ParamBasePageParamsInfo model = arg.Content.DeJson<ParamBasePageParamsInfo>();
            return plugins[model.Platform].DescribeDomainRecords(new DescribeDomainRecordsParamsInfo
            {
                DomainName = model.Domain,
                PageSize = model.PageSize,
                PageNumber = model.PageNumber,
            }, model.Group);
        }
        public bool SetRecordStatus(ClientServiceParamsInfo arg)
        {
            SetRecordStatusParamsInfo model = arg.Content.DeJson<SetRecordStatusParamsInfo>();
            return plugins[model.Platform].SwitchDomainRecordStatus(new SetDomainRecordStatusParamsInfo
            {
                RecordId = model.RecordId,
                Status = model.Status,
                Domain = model.Domain
            }, model.Group);
        }
        public bool DelRecord(ClientServiceParamsInfo arg)
        {
            DelRecordParamsInfo model = arg.Content.DeJson<DelRecordParamsInfo>();
            return plugins[model.Platform].DeleteDomainRecord(new DeleteDomainRecordParamsInfo
            {
                RecordId = model.RecordId,
                Domain = model.Domain
            }, model.Group);
        }
        public bool RemarkRecord(ClientServiceParamsInfo arg)
        {
            RemarkRecordParamsInfo model = arg.Content.DeJson<RemarkRecordParamsInfo>();
            return plugins[model.Platform].UpdateDomainRecordRemark(new UpdateDomainRecordRemarkParamsInfo
            {
                RecordId = model.RecordId,
                Remark = model.Remark,
                Domain = model.Domain
            }, model.Group);
        }
        public List<string> GetRecordTypes(ClientServiceParamsInfo arg)
        {
            return typeof(RecordTypes).GetFields().Where(c => c.FieldType.IsEnum).Select(c => c.Name).ToList();
        }
        public IEnumerable<DescribeSupportLineParamsInfo> GetRecordLines(ClientServiceParamsInfo arg)
        {
            GetRecordLinesParamsInfo model = arg.Content.DeJson<GetRecordLinesParamsInfo>();
            return plugins[model.Platform].DescribeSupportLines(model.Group, model.Domain);
        }
        public bool AddRecord(ClientServiceParamsInfo arg)
        {
            try
            {
                AddRecordParamsInfo model = arg.Content.DeJson<AddRecordParamsInfo>();
                if (string.IsNullOrWhiteSpace(model.RecordId))
                {
                    return plugins[model.Platform].AddDomainRecord(new AddDomainRecordParamsInfo
                    {
                        DomainName = model.DomainName,
                        Line = model.Line,
                        Priority = model.Priority,
                        RR = model.RR,
                        TTL = model.TTL,
                        Type = model.Type,
                        Value = model.Value
                    }, model.Group);
                }
                else
                {
                    return plugins[model.Platform].UpdateDomainRecord(new UpdateDomainRecordParamsInfo
                    {
                        Domain = model.DomainName,
                        RecordId = model.RecordId,
                        Line = model.Line,
                        Priority = model.Priority,
                        RR = model.RR,
                        TTL = model.TTL,
                        Type = model.Type,
                        Value = model.Value
                    }, model.Group);
                }
            }
            catch (Exception ex)
            {
                arg.SetCode(-1, ex.Message);
            }
            return false;
        }
        public async Task<bool> SwitchRecord(ClientServiceParamsInfo arg)
        {
            RecordSwitchParamsInfo model = arg.Content.DeJson<RecordSwitchParamsInfo>();
            var group = config.Platforms.FirstOrDefault(c => c.Name == model.Platform).Groups.FirstOrDefault(c => c.Name == model.Group);
            string record = $"{model.Record}|{model.Domain}";

            if (model.AutoUpdate)
            {
                group.Records.Add(record);
            }
            else
            {
                group.Records.Remove(record);
            }
            group.Records = group.Records.Distinct().ToList();
            await config.SaveConfig();
            return true;
        }

        private void Loop()
        {
            Task.Factory.StartNew(async () =>
            {
                string oldIp = string.Empty;
                while (true)
                {
                    if (config.Enable)
                    {
                        try
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
                        catch (Exception)
                        {

                        }
                    }

                    await Task.Delay(config.Interval);
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
                    foreach (var group in platform.Groups)
                    {
                        var parsedRecords = ParseDomainRecord(group.Records);
                        foreach (var domain in parsedRecords)
                        {
                            //解析记录
                            var records = plugins[platform.Name].DescribeDomainRecords(new DescribeDomainRecordsParamsInfo
                            {
                                DomainName = domain.Key,
                                PageSize = 500
                            }, group.Name).DomainRecords;

                            foreach (var record in records.Where(c => domain.Value.Contains(c.RR)))
                            {
                                try
                                {
                                    var model = new UpdateDomainRecordParamsInfo
                                    {
                                        Domain = domain.Key,
                                        Line = record.Line,
                                        Priority = record.Priority,
                                        RecordId = record.RecordId,
                                        RR = record.RR,
                                        TTL = record.TTL,
                                        Type = record.Type,
                                        Value = ip,
                                    };
                                    plugins[platform.Name].UpdateDomainRecord(model, group.Name);
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
        private Dictionary<string, List<string>> ParseDomainRecord(List<string> records)
        {
            Dictionary<string, List<string>> res = new Dictionary<string, List<string>>();
            foreach (var record in records)
            {
                string[] arr = record.Split('|');
                if (!res.ContainsKey(arr[1]))
                {
                    res.Add(arr[1], new List<string>());
                }
                res[arr[1]].Add(arr[0]);
            }
            return res;
        }
    }
    public class ParamBaseInfo
    {
        public string Platform { get; set; }
        public string Group { get; set; }
        public string Domain { get; set; }
    }
    public class ParamBasePageParamsInfo : ParamBaseInfo
    {
        public long PageNumber { get; set; }
        public long PageSize { get; set; }
    }
    public class SetRecordStatusParamsInfo : ParamBaseInfo
    {
        public string RecordId { get; set; }
        public string Status { get; set; }
    }
    public class DelRecordParamsInfo : ParamBaseInfo
    {
        public string RecordId { get; set; }
    }
    public class RemarkRecordParamsInfo : ParamBaseInfo
    {
        public string RecordId { get; set; }
        public string Remark { get; set; }
    }
    public class GetRecordLinesParamsInfo : ParamBaseInfo
    {
    }
    public class AddRecordParamsInfo : AddDomainRecordParamsInfo
    {
        public string RecordId { get; set; }
        public string Platform { get; set; }
        public string Group { get; set; }
    }

    public class RecordSwitchParamsInfo : ParamBaseInfo
    {
        public string Record { get; set; }
        public bool AutoUpdate { get; set; }
    }
    public class PlatformResultInfo
    {
        public string Name { get; set; }
        public IEnumerable<GroupInfo> Groups { get; set; }

        public class GroupInfo
        {
            public string Name { get; set; }
            public List<string> Records { get; set; }
        }
    }
}
