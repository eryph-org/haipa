﻿using System;
using System.Net.Http;
using Dbosoft.Hosuto.HostedServices;
using Dbosoft.OVN;
using Dbosoft.OVN.Nodes;
using Dbosoft.Rebus;
using Dbosoft.Rebus.Configuration;
using Dbosoft.Rebus.Operations;
using Eryph.Core;
using Eryph.ModuleCore.Networks;
using Eryph.Modules.VmHostAgent.Genetics;
using Eryph.Modules.VmHostAgent.Inventory;
using Eryph.Modules.VmHostAgent.Networks;
using Eryph.Modules.VmHostAgent.Networks.OVS;
using Eryph.Rebus;
using Eryph.VmManagement;
using Eryph.VmManagement.Tracing;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Rebus.Config;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Subscriptions;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;

namespace Eryph.Modules.VmHostAgent
{
    [UsedImplicitly]
    public class VmHostAgentModule
    {
        public string Name => "Eryph.VmHostAgent";

        [UsedImplicitly]
        public void ConfigureServices(IServiceProvider serviceProvider, IServiceCollection services)
        {
            services.Configure<HostOptions>(
                opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(15));


            services.AddHttpClient(GenePoolNames.EryphGenePool, cfg =>
            {
                cfg.BaseAddress = new Uri("https://eryph-staging-b2.b-cdn.net");
            })
                .SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
                .AddPolicyHandler(GetRetryPolicy());


            services.AddSingleton(serviceProvider.GetRequiredService<ISysEnvironment>());
            services.AddSingleton(serviceProvider.GetRequiredService<IOVNSettings>());
            services.AddOvsNode<OVSDbNode>();
            services.AddOvsNode<OVSSwitchNode>();
            services.AddOvsNode<OVNChassisNode>();
        }

        [UsedImplicitly]
        public void AddSimpleInjector(SimpleInjectorAddOptions options)
        {
            options.AddHostedService<SyncService>();
            options.AddHostedService<OVSChassisService>();
            options.AddHostedService<WmiWatcherModuleService>();
            options.AddHostedService<GeneticsRequestWatcherService>();
            options.AddLogging();

            options.Services.AddHostedHandler<StartBusModuleHandler>();

        }

        [UsedImplicitly]
        public void ConfigureContainer(IServiceProvider serviceProvider, Container container)
        {
            container.Register<ISyncClient, SyncClient>();
            container.Register<IHostNetworkCommands<AgentRuntime>, HostNetworkCommands<AgentRuntime>>();
            container.Register<IOVSControl, OVSControl>();
            container.RegisterInstance(serviceProvider.GetRequiredService<INetworkSyncService>());

            container.RegisterSingleton<IFileSystemService, FileSystemService>();
            container.RegisterInstance(serviceProvider.GetRequiredService<IAgentControlService>());

            container.Register<StartBusModuleHandler>();
            container.RegisterSingleton<ITracer, Tracer>();
            container.RegisterSingleton<ITraceWriter, DiagnosticTraceWriter>();

            container.RegisterSingleton<IPowershellEngine, PowershellEngine>();

            container.Register<IVirtualMachineInfoProvider, VirtualMachineInfoProvider>(Lifestyle.Scoped);
            container.RegisterInstance(serviceProvider.GetRequiredService<IVmHostAgentConfigurationManager>());
            container.RegisterInstance(serviceProvider.GetRequiredService<IHostSettingsProvider>());
            container.RegisterInstance(serviceProvider.GetRequiredService<INetworkProviderManager>());
            container.RegisterSingleton<IHostInfoProvider, HostInfoProvider>();

            container.Register<IOVSPortManager, OVSPortManager>(Lifestyle.Scoped);


            var genePoolFactory = new GenePoolFactory(container);
       
            genePoolFactory.Register<LocalGenePoolSource>(GenePoolNames.Local);
            genePoolFactory.Register<RepositoryGenePool>(GenePoolNames.EryphGenePool);
            container.RegisterInstance<IGenePoolFactory>(genePoolFactory);
            container.RegisterSingleton<IGeneProvider, LocalFirstGeneProvider>();
            container.RegisterSingleton<IGeneRequestDispatcher, GeneRequestRegistry>();
            container.RegisterSingleton<IGeneRequestBackgroundQueue, GeneBackgroundTaskQueue>();


            container.RegisterInstance(serviceProvider.GetRequiredService<WorkflowOptions>());
            container.Collection.Register(typeof(IHandleMessages<>), typeof(VmHostAgentModule).Assembly);
            container.AddRebusOperationsHandlers();
            container.RegisterDecorator(typeof(IHandleMessages<>), typeof(TraceDecorator<>));

            var localName = $"{QueueNames.VMHostAgent}.{Environment.MachineName}";
            container.ConfigureRebus(configurer => configurer
                .Transport(t =>
                    serviceProvider.GetService<IRebusTransportConfigurer>()
                        .Configure(t, localName))
                .Options(x =>
                {
                    x.SimpleRetryStrategy(errorDetailsHeaderMaxLength:5);
                    x.SetNumberOfWorkers(5);
                    x.EnableSynchronousRequestReply();
                })
                .Subscriptions(s => 
                    serviceProvider.GetRequiredService<IRebusConfigurer<ISubscriptionStorage>>().Configure(s))
                .Logging(x => x.Serilog()).Start());
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            var delay = Backoff.DecorrelatedJitterBackoffV2(
                TimeSpan.FromSeconds(1), 5);

            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<HttpRequestException>(ex =>
                {
                    return true;
                })
                .WaitAndRetryAsync(6, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2,
                    retryAttempt)));
        }
    }
}