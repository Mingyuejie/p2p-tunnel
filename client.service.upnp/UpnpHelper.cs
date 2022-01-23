using common;
using Mono.Nat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.upnp
{
    public class UpnpHelper
    {
        public List<DeviceInfo> devices = new();

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
                                devices.Add(new DeviceInfo
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
                    TimerIntervalHelper.SetTimeout(() =>
                    {
                        NatUtility.StopDiscovery();
                    }, 5000);
                }
            });

            Logger.Instance.Info("UPNP服务已启动...");
        }
    }

}
