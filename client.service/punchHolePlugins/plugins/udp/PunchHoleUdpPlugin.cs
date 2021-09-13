using common.extends;
using ProtoBuf;
using server.model;
using server.plugin;

namespace client.service.punchHolePlugins.plugins.udp
{
    public class PunchHoleUdpPlugin : IPunchHolePlugin
    {
        private readonly IPunchHoleUdp  punchHoleUdp;
        public PunchHoleUdpPlugin(IPunchHoleUdp punchHoleUdp)
        {
            this.punchHoleUdp = punchHoleUdp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.UDP;

        public void Excute(OnPunchHoleTcpArg arg)
        {
            if (arg.Packet.ServerType != ServerType.UDP) return;

            PunchHoleUdpSteps step = (PunchHoleUdpSteps)arg.Data.PunchStep;
            switch (step)
            {
                case PunchHoleUdpSteps.STEP_1:
                    Step1(arg);
                    break;
                case PunchHoleUdpSteps.STEP_2:
                    Step2(arg);
                    break;
                case PunchHoleUdpSteps.STEP_3:
                    Step3(arg);
                    break;
                case PunchHoleUdpSteps.STEP_4:
                    Step4(arg);
                    break;
                default:
                    break;
            }
        }

        private void Step1(OnPunchHoleTcpArg arg)
        {
            punchHoleUdp.OnStep1(new OnStep1EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }
        private void Step2(OnPunchHoleTcpArg arg)
        {
            punchHoleUdp.OnStep2(new OnStep2EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }

        private void Step3(OnPunchHoleTcpArg arg)
        {
            punchHoleUdp.OnStep3(new OnStep3EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<Step3Model>()
            });
        }
        private void Step4(OnPunchHoleTcpArg arg)
        {
            punchHoleUdp.OnStep4(new OnStep4EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<Step4Model>()
            });
        }
    }

}
