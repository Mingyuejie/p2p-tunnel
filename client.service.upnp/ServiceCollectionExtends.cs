using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace client.service.upnp
{
    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddUpnpPlugin(this ServiceCollection services)
        {
            services.AddSingleton<UpnpHelper>();
            return services;
        }
        public static ServiceProvider UseUpnpPlugin(this ServiceProvider services)
        {
            services.GetService<UpnpHelper>().Start();
            return services;
        }
    }
}
