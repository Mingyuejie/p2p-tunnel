using client.service.punchHolePlugins.plugins;
using client.service.punchHolePlugins.plugins.tcp;
using client.service.punchHolePlugins.plugins.tcp.nutssa;
using client.service.punchHolePlugins.plugins.tcp.nutssb;
using client.service.punchHolePlugins.plugins.udp;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using server.plugin;
using System;

namespace client.service.punchHolePlugins
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

        public MessageTypes MsgType => MessageTypes.SERVER_PUNCH_HOLE;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            PunchHoleModel data = model.Packet.Chunk.DeBytes<PunchHoleModel>();

            if (serverType == ServerType.TCP)
            {
                punchHoldEventHandles.OnPunchHoleTcp(new OnPunchHoleTcpArg
                {
                    Data = data,
                    Packet = model
                });
            }
            else
            {
                punchHoldEventHandles.OnPunchHole(new OnPunchHoleTcpArg
                {
                    Data = data,
                    Packet = model
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
            punchHoldEventHandles.OnReverse(arg);
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
            obj.AddSingleton<PunchHolePlugin>();
            obj.AddSingleton<ReversePlugin>();
            obj.AddSingleton<PunchHoleEventHandles>();

            obj.AddSingleton<IPunchHoleUdp, PunchHoleUdpEventHandles>();
            obj.AddSingleton<PunchHoleUdpPlugin>();


            obj.AddSingleton<PunchHoleTcpNutssAPlugin>();
            obj.AddSingleton<PunchHoleTcpNutssBPlugin>();

            //IP欺骗 
            //obj.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssAEventHandles>();
            //端口复用
            obj.AddSingleton<IPunchHoleTcp, PunchHoleTcpNutssBEventHandles>();
            return obj;
        }
        public static ServiceProvider UsePunchHolePlugin(this ServiceProvider obj)
        {
            Plugin.LoadPlugin(obj.GetService<PunchHolePlugin>());
            obj.GetService<PunchHoleEventHandles>().LoadPlugins();
            return obj;
        }
    }
}
