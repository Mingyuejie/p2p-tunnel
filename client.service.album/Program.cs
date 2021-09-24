using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace client.service.album
{
    public class Program
    {
        static IHost host;
        static CancellationTokenSource cancellationTokenSource;
        public static void Main()
        {
            Run();
        }

        public static void Run()
        {
            cancellationTokenSource = new CancellationTokenSource();
            Task.Run(() =>
            {
                host = CreateHostBuilder(Array.Empty<string>()).Build();
                host.Run();
            }, cancellationTokenSource.Token);
        }

        public static void Stop()
        {
            if (host != null)
            {
                host.StopAsync().Wait();
                host.Dispose();
                host = null;
            }
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var config = AlbumSettingModel.ReadConfig();

            return Host.CreateDefaultBuilder(args).ConfigureLogging((context, loggingBuilder) =>
            {
                foreach (ServiceDescriptor serviceDescriptor in loggingBuilder.Services)
                {
                    if (serviceDescriptor.ImplementationType == typeof(Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider))
                    {
                        loggingBuilder.Services.Remove(serviceDescriptor);
                        break;
                    }
                }
            })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel().ConfigureKestrel((context, options) =>
                    {
                        options.Listen(System.Net.IPAddress.Loopback, config.ServerPort);

                    }).UseStartup<Startup>();
                });
        }

    }
}
