﻿using System;
using Dbosoft.Hosuto.Modules;
using Haipa.Messages;
using Haipa.Modules.ApiProvider;
using Haipa.Modules.ApiProvider.Services;
using Haipa.Rebus;
using Haipa.StateDb;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;

namespace Haipa.Modules
{
    public abstract class ApiModule<TModule> : WebModule where TModule: WebModule
    {
        public abstract string ApiName { get;  }
        public abstract string AudienceName { get; }

        [UsedImplicitly]
        public void ConfigureServices(IServiceProvider serviceProvider, IServiceCollection services)
        {

            services.AddMvc(op =>
            {
            }).AddApiProvider<TModule>(op => op.ApiName = ApiName);


            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = "https://localhost:62189/identity";
                    options.Audience = AudienceName;
                });


            services.AddDbContext<StateStoreContext>(options =>
                serviceProvider.GetRequiredService<IDbContextConfigurer<StateStoreContext>>().Configure(options));

        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseApiProvider(this);
        }

        [UsedImplicitly]
        public void AddSimpleInjector(SimpleInjectorAddOptions options)
        {
            options.AddAspNetCore()
                .AddControllerActivation();
        }

        [UsedImplicitly]
        public virtual void ConfigureContainer(IServiceProvider serviceProvider, Container container)
        {
            container.Collection.Register(typeof(IHandleMessages<>), typeof(TModule).Assembly);
            container.Register<IOperationManager, OperationManager>(Lifestyle.Scoped);

            container.ConfigureRebus(configurer =>
            {
                return configurer
                    .Transport(t =>
                        serviceProvider.GetRequiredService<IRebusTransportConfigurer>().ConfigureAsOneWayClient(t))
                    .Routing(x => x.TypeBased()
                        .Map(MessageTypes.ByRecipient(MessageRecipient.Controllers), QueueNames.Controllers))
                    .Options(x =>
                    {
                        x.SimpleRetryStrategy();
                        x.SetNumberOfWorkers(5);
                    })
                    .Serialization(x => x.UseNewtonsoftJson(new JsonSerializerSettings
                    { TypeNameHandling = TypeNameHandling.None }))
                    .Logging(x => x.ColoredConsole()).Start();
            });
        }

    }
}
