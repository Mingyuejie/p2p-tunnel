using client.servers.clientServer;
using common.extends;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace client.service.tcpforward
{
    public class TcpForwardClientService : IClientService
    {
        private readonly TcpForwardHelper tcpForwardHelper;
        public TcpForwardClientService(TcpForwardHelper tcpForwardHelper)
        {
            this.tcpForwardHelper = tcpForwardHelper;
        }

        public void Add(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            TcpForwardRecordInfoBase fmodel = model.Content.DeJson<TcpForwardRecordInfoBase>();
            string errmsg = tcpForwardHelper.Add(new TcpForwardRecordInfoBase
            {
                AliveType = fmodel.AliveType,
                SourceIp = fmodel.SourceIp,
                SourcePort = fmodel.SourcePort,
                TargetIp = fmodel.TargetIp,
                TargetName = fmodel.TargetName,
                TargetPort = fmodel.TargetPort,
                ID = model.ID,
                Desc = fmodel.Desc
            });
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }

        }

        public void Del(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            string errmsg = tcpForwardHelper.Del(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public List<TcpForwardRecordInfoBase> List(ClientServiceParamsInfo arg)
        {
            return tcpForwardHelper.Mappings;
        }

        public void Start(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            string errmsg = tcpForwardHelper.Start(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public void Stop(ClientServiceParamsInfo arg)
        {
            ForwardSettingParamsInfo model = arg.Content.DeJson<ForwardSettingParamsInfo>();
            tcpForwardHelper.Stop(model.ID);
        }
    }

    

    public class ForwardSettingParamsInfo
    {
        public int ID { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }
}
