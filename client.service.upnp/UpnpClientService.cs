using client.servers.clientServer;
using common;
using common.extends;
using Mono.Nat;
using System;
using System.Collections.Generic;
using System.Linq;

namespace client.service.upnp
{

    public class UpnpClientService : IClientService
    {
        private readonly UpnpHelper upnpHelper;

        public UpnpClientService(UpnpHelper upnpHelper)
        {
            this.upnpHelper = upnpHelper;
        }

        public string[] Devices(ClientServiceParamsInfo arg)
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

        public MappingInfo[] Mappings(ClientServiceParamsInfo arg)
        {
            RequestParamsInfo model = arg.Content.DeJson<RequestParamsInfo>();
            try
            {
                return upnpHelper.devices[model.DeviceIndex].GetMappings().Select(c => new MappingInfo
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
                return Array.Empty<MappingInfo>();
            }
        }

        public void Add(ClientServiceParamsInfo arg)
        {
            MappingInfo model = arg.Content.DeJson<MappingInfo>();
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

        public void Del(ClientServiceParamsInfo arg)
        {
            RequestParamsInfo model = arg.Content.DeJson<RequestParamsInfo>();
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


    public class DeviceInfo
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

    public class RequestParamsInfo
    {
        public int DeviceIndex { get; set; } = 0;
        public int MappingIndex { get; set; } = 0;
    }

    public class MappingInfo
    {
        public int DeviceIndex { get; set; } = 0;

        public int PublicPort { get; set; } = 8099;

        public int PrivatePort { get; set; } = 8099;
        public DateTime Expiration { get; set; } = DateTime.Now;

        public Protocol Protocol { get; set; } = Protocol.Tcp;

        public string Description { get; set; } = string.Empty;

        public int Lifetime { get; set; } = 0;
    }

   
}
