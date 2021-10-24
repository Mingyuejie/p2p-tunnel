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
        /// 解析列表
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        DescribeDomainRecord DescribeDomainRecords(DescribeDomainRecordsModel model, string domain);
        /// <summary>
        /// 添加解析
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool AddDomainRecord(AddDomainRecordModel model, string domain);
        /// <summary>
        /// 删除解析
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool DeleteDomainRecord(DeleteDomainRecordModel model, string domain);
        /// <summary>
        /// 更新解析
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool UpdateDomainRecord(UpdateDomainRecordModel model, string domain);
        /// <summary>
        /// 设置解析状态
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool SetDomainRecordStatus(SetDomainRecordStatusModel model, string domain);
        /// <summary>
        /// 更新解析备注
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        bool UpdateDomainRecordRemark(UpdateDomainRecordRemarkModel model, string domain);
        /// <summary>
        /// 获取解析线路
        /// </summary>
        /// <returns></returns>
        IEnumerable<DescribeSupportLine> DescribeSupportLines(string domain);
    }

    public class AddDomainRecordModel
    {
        public string DomainName { get; set; }
        public string RR { get; set; }
        public RecordType Type { get; set; }
        public string Value { get; set; }
        public long TTL { get; set; }
        public long Priority { get; set; }
        public string Line { get; set; }
    }
    public enum RecordType
    {
        A, NS, MX, TXT, CNAME, SRV, AAAA, CAA, REDIRECT_URL, FORWARD_URL
    }

    public class DeleteDomainRecordModel
    {
        public string RecordId { get; set; }
    }
    public class UpdateDomainRecordModel
    {
        public string RecordId { get; set; }
        public string RR { get; set; }
        public RecordType Type { get; set; }
        public string Value { get; set; }
        public long TTL { get; set; }
        public long Priority { get; set; }
        public string Line { get; set; }
    }
    public class DeleteSubDomainRecordsModel
    {
        public string DomainName { get; set; }
        public string RR { get; set; }
    }
    public class SetDomainRecordStatusModel
    {
        public string RecordId { get; set; }
        public string Status { get; set; } //Enable  //Disable
    }

    public class DescribeSupportLine
    {
        public string LineCode { get; set; }
        public string FatherCode { get; set; }
        public string LineName { get; set; }
        public string LineDisplayName { get; set; }
    }

    public class UpdateDomainRecordRemarkModel
    {
        public string RecordId { get; set; }
        public string Remark { get; set; }
    }

    public class DescribeDomainRecordsModel
    {
        public string DomainName { get; set; }
        public int PageSize { get; set; } = 500;
    }

    public class DescribeDomainRecord
    {
        public IEnumerable<DomainRecordsModel> DomainRecords { get; set; }
        public long PageNumber { get; set; } = 1;
        public long PageSize { get; set; } = 500;

    }

    public class DomainRecordsModel
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
}
