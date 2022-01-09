using common.extends;
using ProtoBuf;
using server.model;
using server.plugin;

namespace client.service.plugins.punchHolePlugins.plugins.udp
{
    public class PunchHoleUdpPlugin : IPunchHolePlugin
    {
        private readonly IPunchHoleUdp punchHoleUdp;
        public PunchHoleUdpPlugin(IPunchHoleUdp punchHoleUdp)
        {
            this.punchHoleUdp = punchHoleUdp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.UDP;

        public void Execute(OnPunchHoleArg arg)
        {
            PunchHoleUdpSteps step = (PunchHoleUdpSteps)arg.Data.PunchStep;
            switch (step)
            {
                case PunchHoleUdpSteps.STEP_1:
                    Step1(arg);
                    break;
                case PunchHoleUdpSteps.STEP_2:
                    Step2(arg);
                    break;
                case PunchHoleUdpSteps.STEP_2_Fail:
                    Step2Fail(arg);
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

        private void Step1(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep1(new OnStep1EventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }
        private void Step2(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep2(new OnStep2EventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }
        private void Step2Fail(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep2Fail(new OnStep2FailEventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }

        private void Step3(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep3(new OnStep3EventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<Step3Model>()
            });
        }
        private void Step4(OnPunchHoleArg arg)
        {
            punchHoleUdp.OnStep4(new OnStep4EventArg
            {
                Packet = arg.Packet,
                RawData = arg.Data,
                Data = arg.Data.Data.DeBytes<Step4Model>()
            });
        }
    }

}
