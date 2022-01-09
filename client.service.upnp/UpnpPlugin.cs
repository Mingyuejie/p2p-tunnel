using client.servers.clientServer;
using common;
using common.extends;
using Microsoft.Extensions.DependencyInjection;
using Mono.Nat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.upnp
{
    public class UpnpHelper
    {
        public List<DeviceModel> devices = new();

        public void Start()
        {
            Task.Run(() =>
            {
                try
                {
                    NatUtility.DeviceFound += (object sender, DeviceEventArgs e) =>
                    {
                        INatDevice device = e.Device;
                        if (device.NatProtocol == NatProtocol.Upnp)
                        {
                            try
                            {
                                devices.Add(new DeviceModel
                                {
                                    Device = device,
                                    Text = $"外网:{device.GetExternalIPAsync().Result},内网:{device.DeviceEndpoint}"
                                });
                            }
                            catch (Exception)
                            {
                            }
                        }
                    };
                    NatUtility.StartDiscovery();
                }
                catch (Exception)
                {
                }
                finally
                {
                    Helper.SetTimeout(() =>
                    {
                        NatUtility.StopDiscovery();
                    }, 5000);
                }
            });

            Logger.Instance.Info("UPNP服务已启动...");
        }
    }

    public class UpnpPlugin : IClientServicePlugin
    {
        private readonly UpnpHelper upnpHelper;

        public UpnpPlugin(UpnpHelper upnpHelper)
        {
            this.upnpHelper = upnpHelper;
        }

        public string[] Devices(ClientServicePluginExecuteWrap arg)
        {
            try
            {
                return upnpHelper.devices.Select(c => c.Text).ToArray();
            }
            catch (Exception ex)
            {
                arg.SetCode(-1, ex.Message);
                return Array.Empty<string>();
            }
        }

        public MappingModel[] Mappings(ClientServicePluginExecuteWrap arg)
        {
            RequestModel model = arg.Content.DeJson<RequestModel>();
            try
            {
                return upnpHelper.devices[model.DeviceIndex].GetMappings().Select(c => new MappingModel
                {
                    Description = c.Description,
                    DeviceIndex = model.DeviceIndex,
                    Lifetime = c.Lifetime,
                    PrivatePort = c.PrivatePort,
                    Protocol = c.Protocol,
                    PublicPort = c.PublicPort,
                    Expiration = c.Expiration
                }).ToArray();
            }
            catch (Exception ex)
            {
                arg.SetCode(-1, ex.Message);
                return Array.Empty<MappingModel>();
            }
        }

        public void Add(ClientServicePluginExecuteWrap arg)
        {
            MappingModel model = arg.Content.DeJson<MappingModel>();
            try
            {
                upnpHelper.devices[model.DeviceIndex].Device.CreatePortMap(new Mapping(model.Protocol, model.PrivatePort, model.PublicPort, model.Lifetime, model.Description));
            }
            catch (Exception ex)
            {
                Logger.Instance.Error(ex.Message);
                arg.SetCode(-1, ex.Message);
            }
        }

        public void Del(ClientServicePluginExecuteWrap arg)
        {
            RequestModel model = arg.Content.DeJson<RequestModel>();
            try
            {
                upnpHelper.devices[model.DeviceIndex].DelMapping(model.MappingIndex);
            }
            catch (Exception ex)
            {
                arg.SetCode(-1, ex.Message);
            }
        }


    }


    public class DeviceModel
    {
        public INatDevice Device { get; set; } = null;
        public string Text { get; set; } = string.Empty;
        public List<Mapping> Mappings { get; set; } = new List<Mapping>();

        public List<Mapping> GetMappings()
        {
            Mappings = Device.GetAllMappings().ToList();
            return Mappings;
        }

        public void DelMapping(int index)
        {
            if (Mappings != null)
            {
                Device.DeletePortMap(Mappings[index]);
            }
        }
    }

    public class RequestModel
    {
        public int DeviceIndex { get; set; } = 0;
        public int MappingIndex { get; set; } = 0;
    }

    public class MappingModel
    {
        public int DeviceIndex { get; set; } = 0;

        public int PublicPort { get; set; } = 8099;

        public int PrivatePort { get; set; } = 8099;
        public DateTime Expiration { get; set; } = DateTime.Now;

        public Protocol Protocol { get; set; } = Protocol.Tcp;

        public string Description { get; set; } = string.Empty;

        public int Lifetime { get; set; } = 0;
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddUpnpPlugin(this ServiceCollection obj)
        {
            obj.AddSingleton<UpnpHelper>();
            return obj;
        }
        public static ServiceProvider UseUpnpPlugin(this ServiceProvider obj)
        {
            obj.GetService<UpnpHelper>().Start();
            return obj;
        }
    }
}
