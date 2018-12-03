using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OK.Bitter.Api.Commands;
using OK.Bitter.Api.HostedServices;
using OK.Bitter.Engine.Managers;
using OK.Bitter.DataAccess;
using OK.Bitter.Engine;
using OK.Bitter.Services;

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

            services.AddSingleton<ISocketHostedService, SocketHostedService>();


            services.AddTransient<AlertCommand>();
            services.AddTransient<AuthCommand>();
            services.AddTransient<HelpCommand>();
            services.AddTransient<MessageCommand>();
            services.AddTransient<PriceCommand>();
            services.AddTransient<ResetCommand>();
            services.AddTransient<SettingCommand>();
            services.AddTransient<StartCommand>();
            services.AddTransient<StatusCommand>();
            services.AddTransient<SubscriptionCommand>();
            services.AddTransient<UserCommand>();

            services.AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

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
            app.UseMvc();
        }
    }
}