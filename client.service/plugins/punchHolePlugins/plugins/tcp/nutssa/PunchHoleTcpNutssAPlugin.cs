using common.extends;
using ProtoBuf;
using server.model;
using server.plugin;

namespace client.service.plugins.punchHolePlugins.plugins.tcp.nutssa
{
    public class PunchHoleTcpNutssAPlugin : IPunchHolePlugin
    {
        private readonly IPunchHoleTcp punchHoleTcp;
        public PunchHoleTcpNutssAPlugin(IPunchHoleTcp punchHoleTcp)
        {
            this.punchHoleTcp = punchHoleTcp;
        }

        public PunchHoleTypes Type => PunchHoleTypes.TCP_NUTSSA;

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
                case PunchHoleTcpNutssBSteps.STEP_3:
                    Step3(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_4:
                    Step4(arg);
                    break;
                case PunchHoleTcpNutssBSteps.STEP_PACKET:
                    StepPacket(arg);
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
        private void StepPacket(OnPunchHoleTcpArg arg)
        {
            punchHoleTcp.OnStepPacket(new OnStepPacketEventArg
            {
                Packet = arg.Packet,
                Data = arg.Data.Data.DeBytes<StepPacketModel>()
            });
        }
    }
}
