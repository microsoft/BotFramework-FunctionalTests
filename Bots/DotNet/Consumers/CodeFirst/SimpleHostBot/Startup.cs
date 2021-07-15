// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Bots;
using Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Dialogs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot
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
        /// <param name="services">The collection of services to add to the container.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();

            // Register the skills configuration class
            services.AddSingleton<SkillsConfiguration>();

            // Configure credentials
            var configCredentialProvider = new ConfigurationCredentialProvider(Configuration);
            services.AddSingleton<ICredentialProvider>(configCredentialProvider);

            services.AddSingleton(sp => new AuthenticationConfiguration
            {
                ClaimsValidator = new AllowedCallersClaimsValidator(
                (from skill in sp.GetService<SkillsConfiguration>().Skills.Values select skill.AppId).ToList())
            });

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
                    sp.GetService<AuthenticationConfiguration>(),
                    null,
                    null));

            // Register the Bot Framework Adapter with error handling enabled.
            // Note: some classes use the base BotAdapter so we add an extra registration that pulls the same instance.
            services.AddSingleton<CloudAdapter, AdapterWithErrorHandler>();
            services.AddSingleton<IBotFrameworkHttpAdapter>(sp => sp.GetService<CloudAdapter>());
            services.AddSingleton<BotAdapter>(sp => sp.GetService<CloudAdapter>());

            // Register the skills client and skills request handler.
            services.AddSingleton<SkillConversationIdFactoryBase, SkillConversationIdFactory>();
            services.AddHttpClient<SkillHttpClient>();
            services.AddSingleton<ChannelServiceHandler, SkillHandler>();
            
            // Register the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Register Conversation state (used by the Dialog system itself).
            services.AddSingleton<ConversationState>();

            // Create SetupDialog
            services.AddSingleton<SetupDialog>();

            // Register the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, HostBot>();

            if (!string.IsNullOrEmpty(Configuration["ChannelService"]))
            {
                // Register a ConfigurationChannelProvider -- this is only for Azure Gov.
                services.AddSingleton<IChannelProvider, ConfigurationChannelProvider>();
            }
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
