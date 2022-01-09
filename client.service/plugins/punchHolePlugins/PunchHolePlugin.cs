using client.service.plugins.punchHolePlugins.plugins.tcp;
using client.service.plugins.punchHolePlugins.plugins.tcp.nutssb;
using client.service.plugins.punchHolePlugins.plugins.udp;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using server.plugin;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace client.service.plugins.punchHolePlugins
{
    public class PunchHolePlugin : IPlugin
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        public PunchHolePlugin(PunchHoleEventHandles punchHoldEventHandles)
        {

            this.punchHoldEventHandles = punchHoldEventHandles;
        }

        public void Execute(PluginParamWrap data)
        {
            PunchHoleModel model = data.Wrap.Memory.DeBytes<PunchHoleModel>();

            punchHoldEventHandles.OnPunchHole(new OnPunchHoleArg
            {
                Data = model,
                Packet = data
            });
        }
    }

    public class ReversePlugin : IPunchHolePlugin
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        public ReversePlugin(PunchHoleEventHandles punchHoldEventHandles)
        {

            this.punchHoldEventHandles = punchHoldEventHandles;
        }

        public PunchHoleTypes Type => PunchHoleTypes.REVERSE;

        public void Execute(OnPunchHoleArg arg)
        {
            punchHoldEventHandles.OnReverse.Push(arg);
        }
    }

    public interface IPunchHolePlugin
    {
        PunchHoleTypes Type { get; }
        void Execute(OnPunchHoleArg arg);
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum PunchHoleTypes
    {
        [Description("UDP打洞")]
        UDP,
        [Description("IP欺骗打洞")]
        TCP_NUTSSA,
        [Description("端口复用打洞")]
        TCP_NUTSSB,
        [Description("反向链接")]
        REVERSE,
    }

    public interface IPunchHoleMessageBase
    {
        PunchHoleTypes PunchType { get; }
        public PunchForwardTypes PunchForwardType { get; }
        public byte PunchStep { get; }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddPunchHolePlugin(this ServiceCollection obj)
        {
            obj.AddPunchHolePlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.AddSingleton<PunchHoleEventHandles>();
            obj.AddSingleton<IPunchHoleUdp, PunchHoleUdpEventHandles>();
            obj.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssBEventHandles>();
            return obj;
        }
        public static ServiceCollection AddPunchHolePlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPunchHolePlugin)));
            foreach (Type item in types)
            {
                obj.AddSingleton(item);
            }
            return obj;
        }

        public static ServiceProvider UsePunchHolePlugin(this ServiceProvider obj)
        {
            obj.GetService<PunchHoleEventHandles>().LoadPlugins();
            return obj;
        }
    }
}
