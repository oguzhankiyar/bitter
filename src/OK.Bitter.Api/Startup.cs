using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Api.HostedServices;
using OK.Bitter.DataAccess;
using OK.Bitter.Engine;
using OK.Bitter.Services;
using OK.GramHook;

namespace OK.Bitter.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDataAccessLayer(_configuration.GetSection("MongoConfigurations")["ConnectionString"]);

            services.AddServicesLayer();

            services.AddEngineLayer();

            services.AddGramHook(opt =>
            {
                opt.BotToken = _configuration.GetSection("ServiceConfigurations:TelegramService")["BotToken"];
            });

            services.AddSingleton<ISocketHostedService, SocketHostedService>();

            services.AddHostedService<ConsumeSocketHostedService>();
            services.AddHostedService<SymbolHostedService>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseGramHook("/api/v1/hooks");
        }
    }
}