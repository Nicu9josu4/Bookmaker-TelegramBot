using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BookmakerTelegramBot.Sevices;


namespace BookmakerTelegramBot
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
            services.AddControllers();
            TelegramClientConfigure TelegramClientRun = new TelegramClientConfigure();
            TelegramClientRun.Controller.connectionString = Configuration.GetConnectionString("DefaultConnection");
            TelegramClientRun.connectionString = Configuration.GetConnectionString("DefaultConnection");
            TelegramClientRun.Token = Configuration.GetValue<string>("TOKEN:DefaultToken");
            _ = TelegramClientRun.StartClient();
            TelegramUpdateClientConfigure TelegramUpdateClientRun = new TelegramUpdateClientConfigure();
            TelegramUpdateClientRun.Controller.connectionString = Configuration.GetConnectionString("DefaultConnection");
            TelegramUpdateClientRun.connectionString = Configuration.GetConnectionString("DefaultConnection");
            TelegramUpdateClientRun.Token = Configuration.GetValue<string>("TOKEN:SecondaryToken");
            _ = TelegramUpdateClientRun.StartClient();
            TelegramLogginingClientConfigure TelegramLogginingClientRun = new TelegramLogginingClientConfigure();
            TelegramLogginingClientRun.Token = Configuration.GetValue<string>("TOKEN:LogMonitorToken");
            TelegramLogginingClientRun.StartClient();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
