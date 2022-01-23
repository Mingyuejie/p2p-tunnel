using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.ddns.platform
{
    public interface IDdnsPlatform
    {
        string Platform { get; }

        /// <summary>
        /// 域名列表
        /// </summary>
        /// <param name="group">本地分组</param>
        /// <returns></returns>
        DescribeDomains DescribeDomains(DescribeDomainsParamsInfo model,string group);
        /// <summary>
        /// 添加域名
        /// </summary>
        /// <param name="group">本地分组</param>
        /// <param name="domain">顶级域名</param>
        /// <returns></returns>
        public bool AddDomain(string group, string domain);

        /// <summary>
        /// 删除域名
        /// </summary>
        /// <param name="group">本地分组</param>
        /// <param name="domain">顶级域名</param>
        /// <returns></returns>
        public bool DeleteDomain(string group, string domain);

        /// <summary>
        /// 解析列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        DescribeDomainRecordsInfo DescribeDomainRecords(DescribeDomainRecordsParamsInfo model, string group);
        /// <summary>
        /// 添加解析
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool AddDomainRecord(AddDomainRecordParamsInfo model, string group);
        /// <summary>
        /// 删除解析
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool DeleteDomainRecord(DeleteDomainRecordParamsInfo model, string group);
        /// <summary>
        /// 更新解析
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool UpdateDomainRecord(UpdateDomainRecordParamsInfo model, string group);
        /// <summary>
        /// 切换解析状态
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool SwitchDomainRecordStatus(SetDomainRecordStatusParamsInfo model, string group);
        /// <summary>
        /// 更新解析备注
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool UpdateDomainRecordRemark(UpdateDomainRecordRemarkParamsInfo model, string group);
        /// <summary>
        /// 获取解析线路
        /// </summary>
        /// <returns></returns>
        IEnumerable<DescribeSupportLineParamsInfo> DescribeSupportLines(string group, string domain);
    }

    public class AddDomainRecordParamsInfo
    {
        public string DomainName { get; set; }
        public string RR { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public long TTL { get; set; }
        public long Priority { get; set; }
        public string Line { get; set; }
    }
    public enum RecordTypes
    {
        A, NS, MX, TXT, CNAME, SRV, AAAA, CAA, REDIRECT_URL, FORWARD_URL
    }

    public class DeleteDomainRecordParamsInfo
    {
        public string RecordId { get; set; }
        public string Domain { get; set; }
    }
    public class UpdateDomainRecordParamsInfo
    {
        public string RecordId { get; set; }
        public string Domain { get; set; }
        public string RR { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public long TTL { get; set; }
        public long Priority { get; set; }
        public string Line { get; set; }
    }
    public class DeleteSubDomainRecordsParamsInfo
    {
        public string DomainName { get; set; }
        public string RR { get; set; }
    }
    public class SetDomainRecordStatusParamsInfo
    {
        public string RecordId { get; set; }
        public string Domain { get; set; }
        public string Status { get; set; } //Enable  //Disable
    }

    public class DescribeSupportLineParamsInfo
    {
        public string LineCode { get; set; }
        public string FatherCode { get; set; }
        public string LineName { get; set; }
        public string LineDisplayName { get; set; }
    }

    public class UpdateDomainRecordRemarkParamsInfo
    {
        public string RecordId { get; set; }
        public string Domain { get; set; }
        public string Remark { get; set; }
    }
    public class DescribeDomainRecordsParamsInfo
    {
        public string DomainName { get; set; }
        public long PageNumber { get; set; } = 1;
        public long PageSize { get; set; } = 500;
    }
    public class DescribeDomainRecordsInfo
    {
        public IEnumerable<DomainRecordsItemInfo> DomainRecords { get; set; }
        public long PageNumber { get; set; } = 1;
        public long PageSize { get; set; } = 500;
        public long Count { get; set; } = 0;

    }
    public class DomainRecordsItemInfo
    {
        public string DomainName { get; set; }
        public string Line { get; set; }
        public bool Locked { get; set; }
        public long Priority { get; set; }
        public string RR { get; set; }
        public string RecordId { get; set; }
        public string Remark { get; set; }
        public string Status { get; set; }
        public long TTL { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public int Weight { get; set; }
    }


    public class DescribeDomains
    {
        public IEnumerable<DescribeDomainItemInfo> Domains { get; set; }
        public long PageNumber { get; set; } = 1;
        public long PageSize { get; set; } = 500;
        public long TotalCount { get; set; } = 0;
    }
    public class DescribeDomainItemInfo
    {
        public string DomainId { get; set; }
        public string DomainName { get; set; }
    }
    public class DescribeDomainsParamsInfo
    {
        public long PageNumber { get; set; } = 1;
        public long PageSize { get; set; } = 500;
    }

    
}
