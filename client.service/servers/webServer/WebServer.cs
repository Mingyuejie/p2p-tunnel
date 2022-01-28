using common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace client.service.servers.webServer
{
    /// <summary>
    /// 本地web管理端服务器
    /// </summary>
    public class WebServer : IWebServer
    {
        private readonly Config config;
        public WebServer(Config config)
        {
            this.config = config;
        }

        public void Start()
        {
            HttpListener http = new HttpListener();
            http.Prefixes.Add($"http://{config.Web.BindIp}:{config.Web.Port}/");
            http.Start();

            _ = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    HttpListenerContext context = http.GetContext();
                    HttpListenerRequest request = context.Request;
                    HttpListenerResponse response = context.Response;
                    Stream stream = response.OutputStream;
                    try
                    {
                        response.Headers["Server"] = "snltty";

                        string path = request.Url.AbsolutePath;
                        //默认页面
                        if (path == "/") path = "index.html";

                        string fullPath = Path.Join(config.Web.Root, path);
                        if (File.Exists(fullPath))
                        {
                            byte[] bytes = File.ReadAllBytes(fullPath);
                            response.ContentLength64 = bytes.Length;
                            response.ContentType = GetContentType(fullPath);
                            stream.Write(bytes, 0, bytes.Length);
                        }
                        else
                        {
                            response.StatusCode = (int)HttpStatusCode.NotFound;
                        }
                    }
                    catch (Exception)
                    {
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                    }
                    stream.Close();
                    stream.Dispose();
                }

            }, TaskCreationOptions.LongRunning);

            Logger.Instance.Info("web服务已启动...");
        }


        private Dictionary<string, string> types = new Dictionary<string, string> {
            { ".png","image/png"},
            { ".jpg","image/jpg"},
            { ".jpeg","image/jpeg"},
            { ".gif","image/gif"},
            { ".svg","image/svg+xml"},
            { ".ico","image/x-icon"},
            { ".js","text/javascript; charset=utf-8"},
            { ".html","text/html; charset=utf-8"},
            { ".css","text/css; charset=utf-8"},
        };
        private string GetContentType(string path)
        {
            string ext = Path.GetExtension(path);
            if (types.ContainsKey(ext))
            {
                return types[ext];
            }
            return "application/octet-stream";
        }
    }

    public static class ServiceCollectionExtends
    {
        public static ServiceCollection AddWebServer(this ServiceCollection services)
        {
            services.AddSingleton<IWebServer, WebServer>();
            return services;
        }
        public static ServiceProvider UseWebServer(this ServiceProvider services)
        {
            services.GetService<IWebServer>().Start();
            return services;
        }
    }
}
