﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.BotFramework;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Authentication;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Bots;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs;
using Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot.Dialogs.Proactive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.BotFrameworkFunctionalTests.TeamsSkillBot
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
            services.AddControllers()
                .AddNewtonsoftJson();

            // Configure credentials.
            services.AddSingleton<ICredentialProvider, ConfigurationCredentialProvider>();
            if (!string.IsNullOrEmpty(Configuration["ChannelService"]))
            {
                // Register a ConfigurationChannelProvider -- this is only for Azure Gov.
                services.AddSingleton<IChannelProvider, ConfigurationChannelProvider>();
            }

            // Register AuthConfiguration to enable custom claim validation.
            services.AddSingleton(sp => new AuthenticationConfiguration { ClaimsValidator = new AllowedCallersClaimsValidator(sp.GetService<IConfiguration>()) });

            // Register the Bot Framework Adapter with error handling enabled.
            // Note: some classes use the base BotAdapter so we add an extra registration that pulls the same instance.
            services.AddSingleton<IBotFrameworkHttpAdapter, SkillAdapterWithErrorHandler>();
            services.AddSingleton<BotAdapter>(sp => sp.GetService<BotFrameworkHttpAdapter>());

            // Create the storage we'll be using for User and Conversation state. (Memory is great for testing purposes.)
            services.AddSingleton<IStorage, MemoryStorage>();

            // Create the Conversation state. (Used by the Dialog system itself.)
            services.AddSingleton<ConversationState>();

            // The Dialog that will be run by the bot.
            services.AddSingleton<ActivityRouterDialog>();

            // The Bot needs an HttpClient to download and upload files. 
            services.AddHttpClient();

            // Create a global hashset for our ConversationReferences
            services.AddSingleton<ConcurrentDictionary<string, ContinuationParameters>>();

            // Create the bot as a transient. In this case the ASP Controller is expecting an IBot.
            services.AddTransient<IBot, SkillBot<ActivityRouterDialog>>();

            // Gives us access to HttpContext so we can create URLs with the host name.
            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseDefaultFiles()
                .UseStaticFiles()

                //.UseWebSockets()
                .UseRouting()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });

            app.UseHttpsRedirection();
        }
    }
}
