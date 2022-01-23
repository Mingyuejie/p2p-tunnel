using common.extends;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TencentCloud.Common;
using TencentCloud.Common.Profile;
using TencentCloud.Dnspod.V20210323;
using TencentCloud.Dnspod.V20210323.Models;

namespace client.service.ddns.platform
{
    public class DdnsPlatformTencent : IDdnsPlatform
    {
        public string Platform => "Tencent";

        private readonly Config config;

        public DdnsPlatformTencent(Config config)
        {
            this.config = config;
        }

        public bool AddDomain(string group, string domain)
        {
            CreateDomainRequest req = new CreateDomainRequest
            {
                Domain = domain
            };

            GetClientByGroup(group).CreateDomainSync(req);
            return true;
        }

        public bool AddDomainRecord(AddDomainRecordParamsInfo model, string group)
        {
            if (model.Priority < 1 || model.Priority > 20)
            {
                model.Priority = 1;
            }

            CreateRecordRequest req = new CreateRecordRequest
            {
                Domain = model.DomainName,
                MX = (ulong)model.Priority,
                RecordLine = model.Line,
                RecordType = model.Type,
                TTL = (ulong)model.TTL,
                Value = model.Value,
                SubDomain = model.RR
            };
            GetClientByGroup(group).CreateRecordSync(req);
            return true;
        }

        public bool DeleteDomain(string group, string domain)
        {
            DeleteDomainRequest req = new DeleteDomainRequest
            {
                Domain = domain,
            };

            GetClientByGroup(group).DeleteDomainSync(req);
            return true;
        }

        public bool DeleteDomainRecord(DeleteDomainRecordParamsInfo model, string group)
        {
            DeleteRecordRequest req = new DeleteRecordRequest
            {
                RecordId = ulong.Parse(model.RecordId),
                Domain = model.Domain
            };

            GetClientByGroup(group).DeleteRecordSync(req);
            return true;
        }

        public DescribeDomainRecordsInfo DescribeDomainRecords(DescribeDomainRecordsParamsInfo model, string group)
        {
            DescribeRecordListRequest req = new DescribeRecordListRequest
            {
                Domain = model.DomainName,
                Offset = (ulong)((model.PageNumber - 1) * model.PageSize),
                Limit = (ulong)model.PageSize
            };

            var resp = GetClientByGroup(group).DescribeRecordListSync(req);

            return new DescribeDomainRecordsInfo
            {
                PageSize = model.PageSize,
                PageNumber = model.PageNumber,
                Count = (long)resp.RecordCountInfo.ListCount,
                DomainRecords = resp.RecordList.Select(c => new DomainRecordsItemInfo
                {
                    DomainName = string.Empty,
                    Line = c.Line,
                    Locked = false,
                    Priority = (long)(c.MX ?? 0),
                    RecordId = c.RecordId.ToString(),
                    Remark = c.Remark,
                    RR = c.Name,
                    Status = c.Status,
                    TTL = (long)(c.TTL ?? 0),
                    Type = c.Type,
                    Value = c.Value,
                    Weight = (int)(c.Weight ?? 0),
                })
            };
        }

        public DescribeDomains DescribeDomains(DescribeDomainsParamsInfo model, string group)
        {
            DescribeDomainListRequest req = new DescribeDomainListRequest
            {
                Offset = ((model.PageNumber - 1) * model.PageSize),
                Limit = model.PageSize,
                
            };

            var resp = GetClientByGroup(group).DescribeDomainListSync(req);
            return new DescribeDomains
            {
                TotalCount = (long)resp.DomainCountInfo.AllTotal,
                PageNumber = model.PageNumber,
                PageSize = model.PageSize,
                Domains = resp.DomainList.Select(c => new DescribeDomainItemInfo
                {
                    DomainId = c.DomainId.ToString(),
                    DomainName = c.Name,
                })
            };
        }

        public IEnumerable<DescribeSupportLineParamsInfo> DescribeSupportLines(string group, string domain)
        {
            DescribeRecordLineListRequest req = new DescribeRecordLineListRequest
            {
                Domain = domain,
                DomainGrade = "DP_ULTRA"
            };

            var resp = GetClientByGroup(group).DescribeRecordLineListSync(req);
            return resp.LineList.Take(10).Select(c => new DescribeSupportLineParamsInfo
            {
                FatherCode = c.Name,
                LineCode = c.Name,
                LineName = c.Name,
                LineDisplayName = c.Name
            });
        }

        private Dictionary<string, string> RecordStatusSwitchMap = new Dictionary<string, string>
        {
            {"ENABLE","DISABLE" },
            {"DISABLE","ENABLE" },
        };
        public bool SwitchDomainRecordStatus(SetDomainRecordStatusParamsInfo model, string group)
        {
            ModifyRecordStatusRequest req = new ModifyRecordStatusRequest
            {
                RecordId = ulong.Parse(model.RecordId),
                Domain = model.Domain,
                Status = RecordStatusSwitchMap[model.Status],
            };

            GetClientByGroup(group).ModifyRecordStatus(req);
            return true;
        }

        public bool UpdateDomainRecord(UpdateDomainRecordParamsInfo model, string group)
        {
            if (model.Priority < 1 || model.Priority > 20)
            {
                model.Priority = 1;
            }

            ModifyRecordRequest req = new ModifyRecordRequest
            {
                RecordId = ulong.Parse(model.RecordId),
                Domain = model.Domain,
                MX = (ulong)model.Priority,
                RecordLine = model.Line,
                RecordType = model.Type,
                TTL = (ulong)model.TTL,
                Value = model.Value,
                SubDomain = model.RR
            };
            GetClientByGroup(group).ModifyRecordSync(req);
            return true;
        }

        public bool UpdateDomainRecordRemark(UpdateDomainRecordRemarkParamsInfo model, string group)
        {
            ModifyRecordRemarkRequest req = new ModifyRecordRemarkRequest
            {
                RecordId = ulong.Parse(model.RecordId),
                Domain = model.Domain,
                Remark = model.Remark
            };

            GetClientByGroup(group).ModifyRecordRemarkSync(req);
            return true;
        }

        private DnspodClient GetClientByGroup(string group)
        {
            return GetClientByGroup(config.Platforms
                .FirstOrDefault(c => c.Name == Platform)
                .Groups.FirstOrDefault(c => c.Name == group));
        }

        private DnspodClient GetClientByGroup(GroupInfo group)
        {
            Credential cred = new Credential
            {
                SecretId = group.Key,
                SecretKey = group.Secret
            };

            ClientProfile clientProfile = new ClientProfile();
            HttpProfile httpProfile = new HttpProfile();
            httpProfile.Endpoint = ("dnspod.tencentcloudapi.com");
            clientProfile.HttpProfile = httpProfile;

            return new DnspodClient(cred, "", clientProfile);
        }
    }
}
