using common.extends;
using server.model;
using server.plugin;

namespace client.service.punchHolePlugins.plugins.tcp.nutssb
{
    public class PunchHoleTcpNutssBPlugin : IPunchHolePlugin
    {
        private readonly IPunchHoleTcp  punchHoleTcp;
        public PunchHoleTcpNutssBPlugin(IPunchHoleTcp punchHoleTcp)
        {
            this.punchHoleTcp = punchHoleTcp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.TCP_NUTSSB;

        public void Excute(OnPunchHoleTcpArg arg)
        {
            if (arg.Packet.ServerType != ServerType.TCP) return;

            PunchHoleTcpNutssBSteps step = (PunchHoleTcpNutssBSteps)arg.Data.PunchStep;
            switch (step)
            {
                case PunchHoleTcpNutssBSteps.STEP_1:
                    Step1(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2:
                    Step2(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_TRY:
                    Step2Try(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_FAIL:
                    Step2Fail(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_2_STOP:
                    Step2Stop(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_3:
                    Step3(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_4:
                    Step4(arg);
                    break;
                default:
                    break;
            }
        }

        private void Step1(OnPunchHoleTcpArg arg)
        {
            punchHoleTcp.OnStep1(new OnStep1EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }
        private void Step2(OnPunchHoleTcpArg arg)
        {
            punchHoleTcp.OnStep2(new OnStep2EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }
        private void Step2Try(OnPunchHoleTcpArg arg)
        {
            punchHoleTcp.OnStep2Retry(new OnStep2RetryEventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<PunchHoleNotifyModel>()
            });
        }
        private void Step2Fail(OnPunchHoleTcpArg arg)
        {
            punchHoleTcp.OnStep2Fail(new OnStep2FailEventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<Step2FailModel>()
            });
        }
        private void Step2Stop(OnPunchHoleTcpArg arg)
        {
            var model = arg.Data.Data.DeBytes<Step2StopModel>();
            punchHoleTcp.OnStep2Stop(model);
        }

        private void Step3(OnPunchHoleTcpArg arg)
        {
            punchHoleTcp.OnStep3(new OnStep3EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<Step3Model>()
            });
        }
        private void Step4(OnPunchHoleTcpArg arg)
        {
            punchHoleTcp.OnStep4(new OnStep4EventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<Step4Model>()
            });
        }
    }


}
