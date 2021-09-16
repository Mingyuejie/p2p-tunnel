using client.service.p2pPlugins.plugins.fileServer;
using client.service.p2pPlugins.plugins.forward.tcp;
using client.service.p2pPlugins.plugins.request;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using ProtoBuf;
using server.model;
using server.models;
using server.plugin;
using System;
using System.Linq;
using System.Reflection;

namespace client.service.p2pPlugins
{
    /// <summary>
    /// p2p消息总线
    /// </summary>
    public class P2PPlugin : IPlugin
    {
        private readonly P2PEventHandles p2PEventHandles;
        public P2PPlugin(P2PEventHandles p2PEventHandles)
        {

            this.p2PEventHandles = p2PEventHandles;
        }

        public MessageTypes MsgType => MessageTypes.P2P;

        public void Excute(PluginExcuteModel model, ServerType serverType)
        {
            P2PModel data = model.Packet.Chunk.DeBytes<P2PModel>();

            if (serverType == ServerType.TCP)
            {
                p2PEventHandles.OnP2PTcp(new OnP2PTcpArg
                {
                    Data = data,
                    Packet = model
                });
            }
            else
            {
                p2PEventHandles.OnP2P(new OnP2PTcpArg
                {
                    Data = data,
                    Packet = model
                });
            }
        }
    }


    /// <summary>
    /// p2p消息插件，用于区别不同的消息类型
    /// </summary>
    public interface IP2PPlugin
    {
        P2PDataTypes Type { get; }

        void Excute(OnP2PTcpArg arg);
    }

    /// <summary>
    /// p2p消息类型
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    [Flags]
    public enum P2PDataTypes
    {
        /// <summary>
        /// 请求
        /// </summary>
        REQUEST,
        /// <summary>
        /// TCO转发
        /// </summary>
        TCP_FORWARD,
        /// <summary>
        /// 文件服务器
        /// </summary>
        FILE_SERVER
    }

    public interface IP2PMessageBase
    {
        P2PDataTypes Type { get; }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddP2PPlugin(this ServiceCollection obj)
        {
            obj.AddP2PPlugin(AppDomain.CurrentDomain.GetAssemblies());

            obj.AddSingleton<P2PEventHandles>();
            obj.AddTcpForwardPlugin();
            obj.AddFileServerPlugin();
            obj.AddRequestPlugin();

            return obj;
        }

        public static ServiceCollection AddP2PPlugin(this ServiceCollection obj, Assembly[] assemblys)
        {
            var types = assemblys.SelectMany(c => c.GetTypes())
                 .Where(c => c.GetInterfaces().Contains(typeof(IP2PPlugin)));
            foreach (var item in types)
            {
                obj.AddSingleton(item);
            }
            return obj;
        }

        public static ServiceProvider UseP2PPlugin(this ServiceProvider obj)
        {
            obj.UseFileServerPlugin();
            obj.UseTcpForwardPlugin();
            obj.UseRequestPlugin();

            obj.UseP2PPlugin(AppDomain.CurrentDomain.GetAssemblies());
            return obj;
        }

        public static ServiceProvider UseP2PPlugin(this ServiceProvider obj, Assembly[] assemblys)
        {
            obj.GetService<P2PEventHandles>().LoadPlugins(assemblys);
            return obj;
        }
    }
}
