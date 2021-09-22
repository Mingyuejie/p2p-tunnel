using client.service.plugins.punchHolePlugins.plugins.tcp;
using client.service.plugins.punchHolePlugins.plugins.tcp.nutssa;
using client.service.plugins.punchHolePlugins.plugins.tcp.nutssb;
using client.service.plugins.punchHolePlugins.plugins.udp;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using server.plugin;
using System;
using System.Linq;
using System.Reflection;

namespace client.service.plugins.punchHolePlugins
{
    /// <summary>
    /// p2p消息总线
    /// </summary>
    public class PunchHolePlugin : IPlugin
    {
        private readonly PunchHoleEventHandles punchHoldEventHandles;
        public PunchHolePlugin(PunchHoleEventHandles punchHoldEventHandles)
        {

            this.punchHoldEventHandles = punchHoldEventHandles;
        }

        public void Excute(PluginParamWrap data)
        {
            PunchHoleModel model = data.Wrap.Content.DeBytes<PunchHoleModel>();

            if (data.ServerType == ServerType.TCP)
            {
                punchHoldEventHandles.OnPunchHoleTcp(new OnPunchHoleTcpArg
                {
                    Data = model,
                    Packet = data
                });
            }
            else
            {
                punchHoldEventHandles.OnPunchHole(new OnPunchHoleTcpArg
                {
                    Data = model,
                    Packet = data
                });
            }
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

        public void Excute(OnPunchHoleTcpArg arg)
        {
            punchHoldEventHandles.OnReverse.Push(arg);
        }
    }


    /// <summary>
    /// p2p消息插件，用于区别不同的消息类型
    /// </summary>
    public interface IPunchHolePlugin
    {
        PunchHoleTypes Type { get; }

        void Excute(OnPunchHoleTcpArg arg);
    }

    /// <summary>
    /// p2p消息类型
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum PunchHoleTypes
    {
        UDP, // UDP打洞
        TCP_NUTSSA, //IP欺骗
        TCP_NUTSSB, //端口复用打洞
        REVERSE, //反向链接
    }

    public interface IPunchHoleMessageBase
    {
        PunchHoleTypes PunchType { get; }
        public PunchForwardTypes PunchForwardType { get; }
        public short PunchStep { get; }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddPunchHolePlugin(this ServiceCollection obj)
        {
            obj.AddPunchHolePlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.AddSingleton<PunchHoleEventHandles>();
            obj.AddSingleton<IPunchHoleUdp, PunchHoleUdpEventHandles>();
            //IP欺骗 
            //obj.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssAEventHandles>();
            //端口复用
            obj.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssBEventHandles>();
            return obj;
        }

        public static ServiceCollection AddPunchHolePlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IPunchHolePlugin)));
            foreach (var item in types)
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
