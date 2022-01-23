using AlibabaCloud.SDK.Alidns20150109.Models;
using System.Collections.Generic;
using System.Linq;

namespace client.service.ddns.platform
{
    public class DdnsPlatformAliyun : IDdnsPlatform
    {
        public string Platform => "Aliyun";

        private readonly Config config;

        public DdnsPlatformAliyun(Config config)
        {
            this.config = config;
        }

        public DescribeDomains DescribeDomains(DescribeDomainsParamsInfo model, string group)
        {
            DescribeDomainsRequest request = new DescribeDomainsRequest
            {
                PageNumber = model.PageNumber,
                PageSize = model.PageSize
            };

            var response = GetClientByGroup(group).DescribeDomains(request);

            return new DescribeDomains
            {
                Domains = response.Body.Domains.Domain.Select(c => new DescribeDomainItemInfo
                {
                    DomainId = c.DomainId,
                    DomainName = c.DomainName
                }),
                PageNumber = response.Body.PageNumber ?? 1,
                PageSize = response.Body.PageSize ?? 50,
                TotalCount = response.Body.TotalCount ?? 0
            };
        }
        public bool AddDomain(string group, string domain)
        {
            AddDomainRequest request = new AddDomainRequest
            {
                DomainName = domain,

            };
            var response = GetClientByGroup(group).AddDomain(request);
            return true;
        }
        public bool DeleteDomain(string group, string domain)
        {
            DeleteDomainRequest request = new DeleteDomainRequest
            {
                DomainName = domain,

            };
            var response = GetClientByGroup(group).DeleteDomain(request);
            return true;
        }

        public bool AddDomainRecord(AddDomainRecordParamsInfo model, string group)
        {
            if (model.Priority < 1 || model.Priority > 50)
            {
                model.Priority = 1;
            }
            AddDomainRecordRequest request = new AddDomainRecordRequest
            {
                DomainName = model.DomainName,
                Line = model.Line,
                Priority = model.Priority,
                RR = model.RR,
                TTL = model.TTL,
                Type = model.Type,
                Value = model.Value
            };
            var response = GetClientByGroup(group).AddDomainRecord(request);
            return true;
        }
        public bool UpdateDomainRecord(UpdateDomainRecordParamsInfo model, string group)
        {
            if (model.Priority < 1 || model.Priority > 50)
            {
                model.Priority = 1;
            }
            UpdateDomainRecordRequest request = new UpdateDomainRecordRequest
            {
                RecordId = model.RecordId,
                Line = model.Line,
                Priority = model.Priority,
                RR = model.RR,
                TTL = model.TTL,
                Type = model.Type,
                Value = model.Value
            };
            var response = GetClientByGroup(group).UpdateDomainRecord(request);
            return true;
        }
        public bool DeleteDomainRecord(DeleteDomainRecordParamsInfo model, string group)
        {
            DeleteDomainRecordRequest request = new DeleteDomainRecordRequest
            {
                RecordId = model.RecordId
            };
            var response = GetClientByGroup(group).DeleteDomainRecord(request);
            return true;
        }

        public DescribeDomainRecordsInfo DescribeDomainRecords(DescribeDomainRecordsParamsInfo model, string group)
        {
            DescribeDomainRecordsRequest request = new DescribeDomainRecordsRequest
            {
                DomainName = model.DomainName,
                PageSize = model.PageSize,
                PageNumber = model.PageNumber
            };
            var response = GetClientByGroup(group).DescribeDomainRecords(request);
            return new DescribeDomainRecordsInfo
            {
                PageSize = response.Body.PageSize ?? 0,
                PageNumber = response.Body.PageNumber ?? 0,
                Count = response.Body.TotalCount ?? 0,
                DomainRecords = response.Body.DomainRecords.Record.Select(c => new DomainRecordsItemInfo
                {
                    DomainName = c.DomainName,
                    Line = c.Line,
                    Locked = c.Locked ?? false,
                    Priority = c.Priority ?? 0,
                    RecordId = c.RecordId,
                    Remark = c.Remark,
                    RR = c.RR,
                    Status = c.Status,
                    TTL = c.TTL ?? 0,
                    Type = c.Type,
                    Value = c.Value,
                    Weight = c.Weight ?? 0
                })
            };
        }

        public IEnumerable<DescribeSupportLineParamsInfo> DescribeSupportLines(string group, string domain)
        {
            DescribeSupportLinesRequest request = new DescribeSupportLinesRequest
            {

            };
            var response = GetClientByGroup(group).DescribeSupportLines(request);
            return response.Body.RecordLines.RecordLine.Take(10).Select(c => new DescribeSupportLineParamsInfo
            {
                FatherCode = c.FatherCode,
                LineCode = c.LineCode,
                LineDisplayName = c.LineDisplayName,
                LineName = c.LineName
            });
        }

        private Dictionary<string, string> RecordStatusSwitchMap = new Dictionary<string, string>
        {
            {"ENABLE","DISABLE" },
            {"DISABLE","ENABLE" },
        };
        public bool SwitchDomainRecordStatus(SetDomainRecordStatusParamsInfo model, string group)
        {
            SetDomainRecordStatusRequest request = new SetDomainRecordStatusRequest
            {
                RecordId = model.RecordId,
                Status = RecordStatusSwitchMap[model.Status]
            };
            var response = GetClientByGroup(group).SetDomainRecordStatus(request);
            return true;
        }

        public bool UpdateDomainRecordRemark(UpdateDomainRecordRemarkParamsInfo model, string group)
        {
            UpdateDomainRecordRemarkRequest request = new UpdateDomainRecordRemarkRequest
            {
                RecordId = model.RecordId,
                Remark = model.Remark
            };
            GetClientByGroup(group).UpdateDomainRecordRemark(request);
            return true;
        }

        private AlibabaCloud.SDK.Alidns20150109.Client GetClientByGroup(string group)
        {
            return GetClientByGroup(config.Platforms
                .FirstOrDefault(c => c.Name == Platform)
                .Groups.FirstOrDefault(c => c.Name == group));
        }

        private AlibabaCloud.SDK.Alidns20150109.Client GetClientByGroup(GroupInfo group)
        {
            return new AlibabaCloud.SDK.Alidns20150109.Client(new AlibabaCloud.OpenApiClient.Models.Config
            {
                AccessKeyId = group.Key,
                AccessKeySecret = group.Secret,
                Endpoint = "dns.aliyuncs.com"
            });
        }

    }
}
