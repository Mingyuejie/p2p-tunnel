// This file is auto-generated, don't edit it. Thanks.

using System.Collections.Generic;
using System.Threading.Tasks;

using Tea;
using Tea.Utils;

using AlibabaCloud.SDK.Alidns20150109.Models;
using System;

namespace AlibabaCloud.SDK.Alidns20150109
{
    public class Client : OpenApiClient.Client
    {

        public Client(OpenApiClient.Models.Config config): base(config)
        {
            this._endpointRule = "central";
            this._endpointMap = new Dictionary<string, string>
            {
                {"ap-northeast-1", "alidns.ap-northeast-1.aliyuncs.com"},
                {"ap-northeast-2-pop", "dns.aliyuncs.com"},
                {"ap-south-1", "alidns.ap-south-1.aliyuncs.com"},
                {"ap-southeast-1", "alidns.ap-southeast-1.aliyuncs.com"},
                {"ap-southeast-2", "alidns.ap-southeast-2.aliyuncs.com"},
                {"ap-southeast-3", "alidns.ap-southeast-3.aliyuncs.com"},
                {"ap-southeast-5", "alidns.ap-southeast-5.aliyuncs.com"},
                {"cn-beijing", "alidns.cn-beijing.aliyuncs.com"},
                {"cn-beijing-finance-1", "dns.aliyuncs.com"},
                {"cn-beijing-finance-pop", "dns.aliyuncs.com"},
                {"cn-beijing-gov-1", "dns.aliyuncs.com"},
                {"cn-beijing-nu16-b01", "dns.aliyuncs.com"},
                {"cn-chengdu", "alidns.cn-chengdu.aliyuncs.com"},
                {"cn-edge-1", "dns.aliyuncs.com"},
                {"cn-fujian", "dns.aliyuncs.com"},
                {"cn-haidian-cm12-c01", "dns.aliyuncs.com"},
                {"cn-hangzhou", "alidns.cn-hangzhou.aliyuncs.com"},
                {"cn-hangzhou-bj-b01", "dns.aliyuncs.com"},
                {"cn-hangzhou-finance", "alidns.cn-hangzhou-finance.aliyuncs.com"},
                {"cn-hangzhou-internal-prod-1", "dns.aliyuncs.com"},
                {"cn-hangzhou-internal-test-1", "dns.aliyuncs.com"},
                {"cn-hangzhou-internal-test-2", "dns.aliyuncs.com"},
                {"cn-hangzhou-internal-test-3", "dns.aliyuncs.com"},
                {"cn-hangzhou-test-306", "dns.aliyuncs.com"},
                {"cn-hongkong", "alidns.cn-hongkong.aliyuncs.com"},
                {"cn-hongkong-finance-pop", "dns.aliyuncs.com"},
                {"cn-huhehaote", "alidns.cn-huhehaote.aliyuncs.com"},
                {"cn-huhehaote-nebula-1", "dns.aliyuncs.com"},
                {"cn-north-2-gov-1", "alidns.cn-north-2-gov-1.aliyuncs.com"},
                {"cn-qingdao", "dns.aliyuncs.com"},
                {"cn-qingdao-nebula", "dns.aliyuncs.com"},
                {"cn-shanghai", "alidns.cn-shanghai.aliyuncs.com"},
                {"cn-shanghai-et15-b01", "dns.aliyuncs.com"},
                {"cn-shanghai-et2-b01", "dns.aliyuncs.com"},
                {"cn-shanghai-finance-1", "alidns.cn-shanghai-finance-1.aliyuncs.com"},
                {"cn-shanghai-inner", "dns.aliyuncs.com"},
                {"cn-shanghai-internal-test-1", "dns.aliyuncs.com"},
                {"cn-shenzhen", "alidns.cn-shenzhen.aliyuncs.com"},
                {"cn-shenzhen-finance-1", "alidns.cn-shenzhen-finance-1.aliyuncs.com"},
                {"cn-shenzhen-inner", "dns.aliyuncs.com"},
                {"cn-shenzhen-st4-d01", "dns.aliyuncs.com"},
                {"cn-shenzhen-su18-b01", "dns.aliyuncs.com"},
                {"cn-wuhan", "dns.aliyuncs.com"},
                {"cn-wulanchabu", "dns.aliyuncs.com"},
                {"cn-yushanfang", "dns.aliyuncs.com"},
                {"cn-zhangbei", "dns.aliyuncs.com"},
                {"cn-zhangbei-na61-b01", "dns.aliyuncs.com"},
                {"cn-zhangjiakou", "alidns.cn-zhangjiakou.aliyuncs.com"},
                {"cn-zhangjiakou-na62-a01", "dns.aliyuncs.com"},
                {"cn-zhengzhou-nebula-1", "dns.aliyuncs.com"},
                {"eu-central-1", "alidns.eu-central-1.aliyuncs.com"},
                {"eu-west-1", "alidns.eu-west-1.aliyuncs.com"},
                {"eu-west-1-oxs", "dns.aliyuncs.com"},
                {"me-east-1", "alidns.me-east-1.aliyuncs.com"},
                {"rus-west-1-pop", "dns.aliyuncs.com"},
                {"us-east-1", "alidns.us-east-1.aliyuncs.com"},
                {"us-west-1", "alidns.us-west-1.aliyuncs.com"},
            };
            CheckConfig(config);
            this._endpoint = GetEndpoint("alidns", _regionId, _endpointRule, _network, _suffix, _endpointMap, _endpoint);
        }


        public string GetEndpoint(string productId, string regionId, string endpointRule, string network, string suffix, Dictionary<string, string> endpointMap, string endpoint)
        {
            if (!TeaUtil.Common.Empty(endpoint))
            {
                return endpoint;
            }
            if (!TeaUtil.Common.IsUnset(endpointMap) && !TeaUtil.Common.Empty(endpointMap.Get(regionId)))
            {
                return endpointMap.Get(regionId);
            }
            return GetEndpointRules(productId, regionId, endpointRule, network, suffix);
        }

        public AddDomainResponse AddDomainWithOptions(AddDomainRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<AddDomainResponse>(DoRPCRequest("AddDomain", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public AddDomainResponse AddDomain(AddDomainRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return AddDomainWithOptions(request, runtime);
        }

        public AddDomainRecordResponse AddDomainRecordWithOptions(AddDomainRecordRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<AddDomainRecordResponse>(DoRPCRequest("AddDomainRecord", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public AddDomainRecordResponse AddDomainRecord(AddDomainRecordRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return AddDomainRecordWithOptions(request, runtime);
        }

        public DeleteDomainResponse DeleteDomainWithOptions(DeleteDomainRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<DeleteDomainResponse>(DoRPCRequest("DeleteDomain", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public DeleteDomainResponse DeleteDomain(DeleteDomainRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return DeleteDomainWithOptions(request, runtime);
        }

        public DeleteDomainRecordResponse DeleteDomainRecordWithOptions(DeleteDomainRecordRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<DeleteDomainRecordResponse>(DoRPCRequest("DeleteDomainRecord", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public DeleteDomainRecordResponse DeleteDomainRecord(DeleteDomainRecordRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return DeleteDomainRecordWithOptions(request, runtime);
        }
     
        public DescribeDomainRecordsResponse DescribeDomainRecordsWithOptions(DescribeDomainRecordsRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
           OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<DescribeDomainRecordsResponse>(DoRPCRequest("DescribeDomainRecords", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public DescribeDomainRecordsResponse DescribeDomainRecords(DescribeDomainRecordsRequest request)
        {
            TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return DescribeDomainRecordsWithOptions(request, runtime);
        }

        public DescribeDomainsResponse DescribeDomainsWithOptions(DescribeDomainsRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
           OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<DescribeDomainsResponse>(DoRPCRequest("DescribeDomains", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public DescribeDomainsResponse DescribeDomains(DescribeDomainsRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return DescribeDomainsWithOptions(request, runtime);
        }
     
        public DescribeSupportLinesResponse DescribeSupportLinesWithOptions(DescribeSupportLinesRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<DescribeSupportLinesResponse>(DoRPCRequest("DescribeSupportLines", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public DescribeSupportLinesResponse DescribeSupportLines(DescribeSupportLinesRequest request)
        {
            TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return DescribeSupportLinesWithOptions(request, runtime);
        }

        public SetDomainRecordStatusResponse SetDomainRecordStatusWithOptions(SetDomainRecordStatusRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<SetDomainRecordStatusResponse>(DoRPCRequest("SetDomainRecordStatus", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public SetDomainRecordStatusResponse SetDomainRecordStatus(SetDomainRecordStatusRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return SetDomainRecordStatusWithOptions(request, runtime);
        }
    
    
        public UpdateDomainRecordResponse UpdateDomainRecordWithOptions(UpdateDomainRecordRequest request, AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<UpdateDomainRecordResponse>(DoRPCRequest("UpdateDomainRecord", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public UpdateDomainRecordResponse UpdateDomainRecord(UpdateDomainRecordRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return UpdateDomainRecordWithOptions(request, runtime);
        }

        public UpdateDomainRecordRemarkResponse UpdateDomainRecordRemarkWithOptions(UpdateDomainRecordRemarkRequest request, TeaUtil.Models.RuntimeOptions runtime)
        {
            TeaUtil.Common.ValidateModel(request);
            OpenApiClient.Models.OpenApiRequest req = new OpenApiClient.Models.OpenApiRequest
            {
                Body = TeaUtil.Common.ToMap(request),
            };
            return TeaModel.ToObject<UpdateDomainRecordRemarkResponse>(DoRPCRequest("UpdateDomainRecordRemark", "2015-01-09", "HTTPS", "POST", "AK", "json", req, runtime));
        }

        public UpdateDomainRecordRemarkResponse UpdateDomainRecordRemark(UpdateDomainRecordRemarkRequest request)
        {
            AlibabaCloud.TeaUtil.Models.RuntimeOptions runtime = new AlibabaCloud.TeaUtil.Models.RuntimeOptions();
            return UpdateDomainRecordRemarkWithOptions(request, runtime);
        }


        public static string GetEndpointRules(string product, string regionId, string endpointType, string network, string suffix)
        {
            string text = string.Empty;
            product = ((product == null) ? "" : product);
            if (endpointType == "regional")
            {
                if (string.IsNullOrEmpty(regionId))
                {
                    throw new ArgumentException("RegionId is empty, please set a valid RegionId", "RegionId");
                }
                text = "<product><suffix><network>.<region_id>.aliyuncs.com".Replace("<region_id>", regionId);
            }
            else
            {
                text = "<product><suffix><network>.aliyuncs.com";
            }
            text = text.Replace("<product>", product.ToLower());
            if (string.IsNullOrEmpty(network) || network == "public")
            {
                text = text.Replace("<network>", "");
            }
            else
            {
                text = text.Replace("<network>", "-" + network);
            }
            if (string.IsNullOrEmpty(suffix))
            {
                text = text.Replace("<suffix>", "");
            }
            else
            {
                text = text.Replace("<suffix>", "-" + suffix);
            }
            return text;
        }

    }
}
