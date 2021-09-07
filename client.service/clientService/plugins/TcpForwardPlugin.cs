using client.service.p2pPlugins.plugins.forward.tcp;
using common;
using common.extends;

namespace client.service.clientService.plugins
{
    public class TcpForwardPlugin : IClientServicePlugin
    {
        public void Add(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();

            TcpForwardRecordBaseModel fmodel = arg.Content.DeJson<TcpForwardRecordBaseModel>();
            string errmsg = TcpForwardHelper.Instance.Add(new TcpForwardRecordBaseModel
            {
                AliveType = fmodel.AliveType,
                Listening = false,
                SourceIp = fmodel.SourceIp,
                SourcePort = fmodel.SourcePort,
                TargetIp = fmodel.TargetIp,
                TargetName = fmodel.TargetName,
                TargetPort = fmodel.TargetPort
            }, model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }

        }

        public void Del(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = TcpForwardHelper.Instance.Del(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public System.Collections.Generic.List<TcpForwardRecordBaseModel> List(ClientServicePluginExcuteWrap arg)
        {
            return TcpForwardHelper.Instance.Mappings;
        }

        public void Start(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            string errmsg = TcpForwardHelper.Instance.Start(model.ID);
            if (!string.IsNullOrWhiteSpace(errmsg))
            {
                arg.SetCode(-1, errmsg);
            }
        }

        public void Stop(ClientServicePluginExcuteWrap arg)
        {
            ForwardSettingModel model = arg.Content.DeJson<ForwardSettingModel>();
            TcpForwardHelper.Instance.Stop(model.ID);
        }
    }

    public class ForwardSettingModel
    {
        public int ID { get; set; } = 0;
        public string Content { get; set; } = string.Empty;
    }
}
