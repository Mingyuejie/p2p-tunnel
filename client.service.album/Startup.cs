using client.service.album.db;
using client.service.album.filters;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace client.service.album
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(typeof(DBHelper<>));

            var config = AlbumSettingModel.ReadConfig();
            services.AddSingleton((e)=> config);
            services.AddControllers(options =>
            {
                options.Filters.Add(typeof(WebApiResultMiddleware));
                options.Filters.Add(typeof(WebApiExceptionMiddleware));
            });
            services.AddCors((options) =>
            {
                options.AddDefaultPolicy((policy) =>
                {
                    policy.AllowCredentials().AllowAnyHeader().AllowAnyMethod().SetPreflightMaxAge(TimeSpan.FromMinutes(30)).SetIsOriginAllowed(_ => true);
                });
            });

            services.AddSingleton<VerifyCaching>();
            services.AddMemoryCache();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            if (string.IsNullOrWhiteSpace(env.WebRootPath))
            {
                env.WebRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors();
           

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
