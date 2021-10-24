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

        public bool AddDomainRecord(AddDomainRecordModel model, string domain)
        {
            AddDomainRecordRequest request = new AddDomainRecordRequest
            {
                DomainName = model.DomainName,
                Line = model.Line,
                Priority = model.Priority,
                RR = model.RR,
                TTL = model.TTL,
                Type = model.Type.ToString(),
                Value = model.Value
            };
            var response = GetClient(domain).AddDomainRecord(request);
            return true;
        }

        public bool DeleteDomainRecord(DeleteDomainRecordModel model, string domain)
        {
            DeleteDomainRecordRequest request = new DeleteDomainRecordRequest
            {
                RecordId = model.RecordId
            };
            var response = GetClient(domain).DeleteDomainRecord(request);
            return true;
        }

        public DescribeDomainRecord DescribeDomainRecords(DescribeDomainRecordsModel model, string domain)
        {
            DescribeDomainRecordsRequest request = new DescribeDomainRecordsRequest
            {
                DomainName = model.DomainName,
                PageSize = model.PageSize
            };
            var response = GetClient(domain).DescribeDomainRecords(request);
            return new DescribeDomainRecord
            {
                PageSize = response.Body.PageSize ?? 0,
                PageNumber = response.Body.PageNumber ?? 0,
                DomainRecords = response.Body.DomainRecords.Record.Select(c => new DomainRecordsModel
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

        public IEnumerable<DescribeSupportLine> DescribeSupportLines(string domain)
        {
            DescribeSupportLinesRequest request = new DescribeSupportLinesRequest
            {

            };
            var response = GetClient(domain).DescribeSupportLines(request);
            return response.Body.RecordLines.RecordLine.Select(c => new DescribeSupportLine
            {
                FatherCode = c.FatherCode,
                LineCode = c.LineCode,
                LineDisplayName = c.LineDisplayName,
                LineName = c.LineName
            });
        }

        public bool SetDomainRecordStatus(SetDomainRecordStatusModel model, string domain)
        {
            SetDomainRecordStatusRequest request = new SetDomainRecordStatusRequest
            {
                RecordId = model.RecordId,
                Status = model.Status
            };
            var response = GetClient(domain).SetDomainRecordStatus(request);
            return true;
        }

        public bool UpdateDomainRecord(UpdateDomainRecordModel model, string domain)
        {
            UpdateDomainRecordRequest request = new UpdateDomainRecordRequest
            {
                RecordId = model.RecordId,
                Line = model.Line,
                Priority = model.Priority,
                RR = model.RR,
                TTL = model.TTL,
                Type = model.Type.ToString(),
                Value = model.Value
            };
            var response = GetClient(domain).UpdateDomainRecord(request);
            return true;
        }

        public bool UpdateDomainRecordRemark(UpdateDomainRecordRemarkModel model, string domain)
        {
            UpdateDomainRecordRemarkRequest request = new UpdateDomainRecordRemarkRequest
            {
                RecordId = model.RecordId,
                Remark = model.Remark
            };
            GetClient(domain).UpdateDomainRecordRemark(request);
            return true;
        }

        private AlibabaCloud.SDK.Alidns20150109.Client GetClient(string domain)
        {
            var group = config.Platforms
                .FirstOrDefault(c => c.Name == Platform)
                .Groups.FirstOrDefault(c => c.Domains.FirstOrDefault(c => c.Domain == domain) != null);

            return new AlibabaCloud.SDK.Alidns20150109.Client(new AlibabaCloud.OpenApiClient.Models.Config
            {
                AccessKeyId = group.Key,
                AccessKeySecret = group.Secret,
                Endpoint = "dns.aliyuncs.com"
            });
        }
    }
}
