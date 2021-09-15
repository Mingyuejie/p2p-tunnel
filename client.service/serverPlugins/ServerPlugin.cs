using client.service.events;
using client.service.p2pPlugins;
using client.service.punchHolePlugins;
using client.service.serverPlugins.clients;
using client.service.serverPlugins.heart;
using client.service.serverPlugins.register;
using client.service.serverPlugins.reset;
using Microsoft.Extensions.DependencyInjection;
using server;
using server.plugin;

namespace client.service.serverPlugins
{
    public static class ServerPlugin
    {
        public static ServiceCollection AddServerPlugin(this ServiceCollection obj)
        {
            obj.AddSingleton<ITcpServer, TCPServer>();
            obj.AddSingleton<IUdpServer, UDPServer>();
            obj.AddSingleton<EventHandlers>();

            obj.AddSingleton<ResetEventHandles>();
            obj.AddSingleton<ResetPlugin>();

            obj.AddSingleton<HeartEventHandles>();
            obj.AddSingleton<HeartPlugin>();

            obj.AddSingleton<ClientsHelper>();
            obj.AddSingleton<ClientsEventHandles>();
            obj.AddSingleton<ServerSendClientsPlugin>();

            obj.AddSingleton<RegisterEventHandles>();
            obj.AddSingleton<RegisterHelper>();
            obj.AddSingleton<RegisterState>();
            obj.AddSingleton<RegisterResultPlugin>();


            obj.AddP2PPlugin();
            obj.AddPunchHolePlugin();

            return obj;
        }

        public static ServiceProvider UseServerPlugin(this ServiceProvider obj)
        {
            Plugin.LoadPlugin(obj.GetService<ResetPlugin>());
            Plugin.LoadPlugin(obj.GetService<RegisterResultPlugin>());
            Plugin.LoadPlugin(obj.GetService<HeartPlugin>());
            Plugin.LoadPlugin(obj.GetService<ServerSendClientsPlugin>());

            obj.GetService<ClientsHelper>();
            
            obj.UseP2PPlugin();
            obj.UsePunchHolePlugin();

            return obj;
        }
    }
}
