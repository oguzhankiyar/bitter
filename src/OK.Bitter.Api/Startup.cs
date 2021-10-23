using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OK.Bitter.Api.Config;
using OK.Bitter.DataAccess;
using OK.Bitter.Engine;
using OK.Bitter.Services;
using OK.GramHook;

namespace OK.Bitter.Api
{
    public class Startup
    {
        private readonly IConfiguration _configuration;
        private readonly BitterConfig _bitterConfig;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
            _bitterConfig = new BitterConfig();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<KestrelServerOptions>(opts =>
            {
                opts.AllowSynchronousIO = true;
            });
            
            _configuration.Bind(_bitterConfig);

            services.AddSingleton(_bitterConfig);

            services.AddDataAccessLayer(_bitterConfig.DataAccess);

            services.AddTelegramMessageService(_bitterConfig.Telegram);
            services.AddIFTTTCallService(_bitterConfig.IFTTT);

            services.AddEngineLayer();

            services.AddGramHook(opt =>
            {
                opt.BotToken = _bitterConfig.Telegram.BotToken;
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
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

            app.UseGramHook(_bitterConfig.HookPath);
        }
    }
}