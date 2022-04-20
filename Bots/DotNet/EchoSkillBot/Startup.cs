// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.FunctionalTestsBots.EchoSkillBot.Bots;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Bot.Builder.FunctionalTestsBots.EchoSkillBot
{
    public class Startup
    {
        private const string CallersConfigKey = "AllowedCallers";

        public Startup(IConfiguration config)
        {
            Configuration = config;
        }

        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">Method to add services to the container.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            services.AddSingleton(sp =>
            {
                // AllowedCallers is the setting in the appsettings.json file that consists of the list of parent bot IDs that are allowed to access the skill.
                // To add a new parent bot, simply edit the AllowedCallers and add the parent bot's Microsoft app ID to the list.
                // In this sample, we allow all callers if AllowedCallers contains an "*".
                var callersSection = Configuration.GetSection(CallersConfigKey);
                var callers = callersSection.Get<string[]>();
                if (callers == null)
                {
                    throw new ArgumentNullException($"\"{CallersConfigKey}\" not found in configuration.");
                }

                return new AuthenticationConfiguration { ClaimsValidator = new AllowedCallersClaimsValidator(callers) };
            });

            services.AddSingleton<BotFrameworkAuthentication, ConfigurationBotFrameworkAuthentication>();

            // Create the Bot Framework Adapter with error handling enabled.
            services.AddSingleton<IBotFrameworkHttpAdapter, SkillAdapterWithErrorHandler>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, EchoBot>();
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application request pipeline to be configured.</param>
        /// <param name="env">The web hosting environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
        }
    }
}
