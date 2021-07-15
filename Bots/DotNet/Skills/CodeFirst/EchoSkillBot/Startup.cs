// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.EchoSkillBot.Bots;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBot
{
    public class Startup
    {
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

            var configCredentialProvider = new ConfigurationCredentialProvider(Configuration);

            services.AddSingleton(sp => BotFrameworkAuthenticationFactory.Create(
                    new ConfigurationChannelProvider(Configuration).ChannelService ?? string.Empty, 
                    true,
                    AuthenticationConstants.ToChannelFromBotLoginUrl,
                    AuthenticationConstants.ToChannelFromBotOAuthScope,
                    AuthenticationConstants.ToBotFromChannelTokenIssuer,
                    AuthenticationConstants.OAuthUrl,
                    AuthenticationConstants.ToBotFromChannelOpenIdMetadataUrl,
                    AuthenticationConstants.ToBotFromEmulatorOpenIdMetadataUrl,
                    CallerIdConstants.PublicAzureChannel,
                    new PasswordServiceClientCredentialFactory(
                        configCredentialProvider.AppId,
                        configCredentialProvider.Password,
                        null,
                        null),
                    new AuthenticationConfiguration
                    {
                        ClaimsValidator = new AllowedCallersClaimsValidator(new List<string>(Configuration.GetSection("AllowedCallers").Get<string[]>()))
                    },
                    null,
                    null));

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
