﻿using System;
using Eryph.ModuleCore;
using Eryph.Modules.AspNetCore;
using Eryph.Modules.AspNetCore.ApiProvider.Handlers;
using Eryph.Modules.ComputeApi.Handlers;
using Eryph.Modules.ComputeApi.Model;
using Eryph.Modules.ComputeApi.Model.V1;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using SimpleInjector;
using IEndpointResolver = Eryph.ModuleCore.IEndpointResolver;

namespace Eryph.Modules.ComputeApi
{
    [UsedImplicitly]
    public class ComputeApiModule : ApiModule<ComputeApiModule>
    {
        private readonly IEndpointResolver _endpointResolver;

        public ComputeApiModule(IEndpointResolver endpointResolver)
        {
            _endpointResolver = endpointResolver;
        }

        public override string Path => _endpointResolver.GetEndpoint("compute").ToString();

        public override string ApiName => "Compute Api";
        public override string AudienceName => "compute_api";

        public override void ConfigureServices(IServiceProvider serviceProvider, IServiceCollection services, IHostEnvironment env)
        {
            base.ConfigureServices(serviceProvider, services, env);

            var endpointResolver = serviceProvider.GetRequiredService<IEndpointResolver>();
            var authority = endpointResolver.GetEndpoint("identity").ToString();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("compute:catlets:read",
                    policy => policy.Requirements.Add(new HasScopeRequirement(
                        authority,
                        "compute:catlets:read", "compute:catlets:write", "compute:read", "compute:write")));
                options.AddPolicy("compute:catlets:write",
                    policy => policy.Requirements.Add(new HasScopeRequirement(
                        authority,
                        "compute:catlets:write", "compute:write")));

                options.AddPolicy("compute:catlets:start",
                    policy => policy.Requirements.Add(new HasScopeRequirement(
                        authority,
                        "compute:catlets:start", "compute:catlets:write","compute:write")));

                options.AddPolicy("compute:catlets:stop",
                    policy => policy.Requirements.Add(new HasScopeRequirement(
                        authority,
                        "compute:catlets:stop", "compute:catlets:write", "compute:write")));
            });
        }

        public override void ConfigureContainer(IServiceProvider serviceProvider, Container container)
        {
            container.Register<IGetRequestHandler<StateDb.Model.VirtualCatlet, VirtualCatletConfiguration>, 
                GetVirtualCatletConfigurationHandler>();

            container.Register<IGetRequestHandler<StateDb.Model.Project, VirtualNetworkConfiguration>,
                GetVirtualNetworksConfigurationHandler>();


            base.ConfigureContainer(serviceProvider, container);
        }
    }

}