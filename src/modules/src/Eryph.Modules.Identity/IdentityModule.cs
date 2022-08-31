﻿using System;
using System.Collections.Generic;
using Dbosoft.Hosuto.Modules;
using Dbosoft.IdentityServer.Configuration;
using Dbosoft.IdentityServer.Configuration.DependencyInjection;
using Dbosoft.IdentityServer.Configuration.DependencyInjection.BuilderExtensions;
using Dbosoft.IdentityServer.EfCore;
using Dbosoft.IdentityServer.EfCore.Storage.DbContexts;
using Dbosoft.IdentityServer.Models;
using Dbosoft.IdentityServer.Storage.Models;
using Eryph.IdentityDb;
using Eryph.ModuleCore;
using Eryph.Modules.AspNetCore;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.Identity.Services;
using Eryph.Security.Cryptography;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;
using Client = Eryph.Modules.Identity.Models.V1.Client;
using Scope = Dbosoft.IdentityServer.Storage.Models.ApiScope;

namespace Eryph.Modules.Identity
{
    /// <summary>
    ///     Defines the <see cref="IdentityModule" />
    /// </summary>
    [ApiVersion("1.0")]
    public class IdentityModule : WebModule
    {
        private readonly IEndpointResolver _endpointResolver;

        public IdentityModule(IEndpointResolver endpointResolver)
        {
            _endpointResolver = endpointResolver;
        }

        public override string Path => _endpointResolver.GetEndpoint("identity").ToString();

        public void AddSimpleInjector(SimpleInjectorAddOptions options)
        {
            options.AddAspNetCore()
                .AddControllerActivation();
        }

        public void ConfigureServices(IServiceProvider serviceProvider, IServiceCollection services,
            IHostEnvironment env)
        {
            var endpointResolver = serviceProvider.GetRequiredService<IEndpointResolver>();
            var authority = endpointResolver.GetEndpoint("identity").ToString();

            services.AddMvc()
                .AddApiProvider<IdentityModule>(op => op.ApiName = "Eryph Identity Api");

            //services.AddSingleton<IModelConfiguration, ODataModelConfiguration>();

            services.AddIdentityServer(options =>
                {
                    //options.Events.RaiseSuccessEvents = true;
                    //options.Events.RaiseFailureEvents = true;
                    //options.Events.RaiseErrorEvents = true;
                    //options.Events.RaiseInformationEvents = true;
                })
                .AddJwtBearerClientAuthentication()
                .AddDeveloperSigningCredential()
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        serviceProvider.GetRequiredService<IDbContextConfigurer<ConfigurationDbContext>>()
                            .Configure(builder);
                })
                .AddInMemoryApiResources(new List<ApiResource>
                {
                    new ApiResource("common_api")
                    {
                        Scopes = new List<string>
                        {
                            "compute_api"
                        }
                    },

                    new ApiResource("compute_api")
                    {
                        Scopes = new List<string>
                        {
                            "common_api"
                        }
                    },

                    new ApiResource
                    {
                        Name = "identity_api",
                        Scopes = new List<string>
                        {
                            "identity:clients:write:all",
                            "identity:clients:read:all"
                        },

                    }
                })
                .AddInMemoryApiScopes(new[]
                {
                    new Scope("common_api"),
                    new Scope("compute_api"),

                    new Scope("identity:clients:write:all", "Full access to clients"),
                    new Scope("identity:clients:read:all", "Read only access to clients")
                })
                //.AddInMemoryCaching()
                .AddInMemoryIdentityResources(
                    new[]
                    {
                        new IdentityResources.OpenId()
                    });


            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.Authority = authority;
                    options.Audience = "identity_api";
                });


            services.AddAuthorization(options =>
            {
                options.AddPolicy("identity:clients:read:all",
                    policy => policy.Requirements.Add(new HasScopeRequirement(
                        authority,
                        "identity:clients:read:all", "identity:clients:write:all")));
            });
            services.AddAuthorization(options =>
            {
                options.AddPolicy("identity:clients:write:all",
                    policy => policy.Requirements.Add(new HasScopeRequirement(
                        authority,
                        "identity:clients:write:all")));
            });
        }


        public void Configure(IApplicationBuilder app)
        {
            app.UseIdentityServer();
            app.UseApiProvider(this);
        }

        [UsedImplicitly]
        public void ConfigureContainer(IServiceProvider sp, Container container)
        {
            container.Register<IRSAProvider, RSAProvider>();
            container.Register<ICertificateGenerator, CertificateGenerator>();
            container.Register(sp.GetRequiredService<IEndpointResolver>);
            container.Register<IClientRepository, ClientRepository<ConfigurationDbContext>>(Lifestyle.Scoped);
            container.Register<IIdentityServerClientService, IdentityServerClientService>(Lifestyle.Scoped);
            container.Register<IClientService<Client>, ClientService<Client>>(Lifestyle.Scoped);
        }
    }
}