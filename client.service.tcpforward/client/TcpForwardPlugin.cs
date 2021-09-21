using client.servers.clientServer;
using common.extends;

namespace client.service.tcpforward.client
{
    public class TcpForwardPlugin : IClientServicePlugin
    {
        private readonly TcpForwardHelper tcpForwardHelper;
        public TcpForwardPlugin(TcpForwardHelper tcpForwardHelper)
        {
            this.tcpForwardHelper = tcpForwardHelper;
        }

        public void Add(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            TcpForwardRecordBaseModel fmodel = model.Content.DeJson<TcpForwardRecordBaseModel>();
            string errmsg = tcpForwardHelper.Add(new TcpForwardRecordBaseModel
            {
                AliveType = fmodel.AliveType,
                Listening = false,
                SourceIp = fmodel.SourceIp,
                SourcePort = fmodel.SourcePort,
                TargetIp = fmodel.TargetIp,
                TargetName = fmodel.TargetName,
                TargetPort = fmodel.TargetPort,
                ID = model.ID
            });
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }

        }

        public void Del(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = tcpForwardHelper.Del(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public System.Collections.Generic.List<TcpForwardRecordBaseModel> List(ClientServicePluginExcuteWrap arg)
        {
            return tcpForwardHelper.Mappings;
        }

        public void Start(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = tcpForwardHelper.Start(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            tcpForwardHelper.Stop(model.ID);
        }
    }

    public class TcpForwardPushMsgPlugin : IClientServerPushMsgPlugin
    {
        private readonly TcpForwardHelper tcpForwardHelper;
        public TcpForwardPushMsgPlugin(TcpForwardHelper tcpForwardHelper)
        {
            this.tcpForwardHelper = tcpForwardHelper;
        }

        public object List()
        {
            return tcpForwardHelper.Mappings;
        }
    }

    public class ForwardSettingModel
    {
        public int ID { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }
}
