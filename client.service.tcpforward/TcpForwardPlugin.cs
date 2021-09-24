﻿using client.servers.clientServer;
using client.service.tcpforward.client;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using server.model;
using server.plugin;
using System;
using System.IO;

namespace client.service.tcpforward
{
    public class TcpForwardPlugin : IPlugin
    {
        private readonly TcpForwardEventHandles tcpForwardEventHandles;
        public TcpForwardPlugin(TcpForwardEventHandles tcpForwardEventHandles)
        {
            this.tcpForwardEventHandles = tcpForwardEventHandles;
        }

        public void Excute(PluginParamWrap arg)
        {
            TcpForwardModel data = arg.Wrap.Content.DeBytes<TcpForwardModel>();
            tcpForwardEventHandles.OnTcpForward(new OnTcpForwardEventArg
            {
                Packet = arg,
                Data = data,
            });
        }
    }


    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddTcpForwardPlugin(this ServiceCollection obj)
        {
            TcpForwardSettingModel config = TcpForwardSettingModel.ReadConfig();
            obj.AddSingleton((e) => config);

            obj.AddSingleton<TcpForwardServer>();
            obj.AddSingleton<TcpForwardHelper>();
            obj.AddSingleton<TcpForwardEventHandles>();
            return obj;
        }
        public static ServiceProvider UseTcpForwardPlugin(this ServiceProvider obj)
        {
            obj.GetService<TcpForwardHelper>().Start();

            return obj;
        }
    }
}