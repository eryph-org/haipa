﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eryph.Configuration;
using Eryph.Modules.Controller;
using Eryph.StateDb;
using Eryph.ZeroState.Networks;
using Eryph.ZeroState.VirtualMachines;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;

namespace Eryph.ZeroState
{
    public static class ZeroStateContainerExtensions
    {
        public static void UseZeroState(this Container container)
        {
            // TODO move file system registration somewhere else
            container.RegisterSingleton<IFileSystem, FileSystem>();
            container.Register(typeof(IZeroStateQueue<>), typeof(ZeroStateQueue<>), Lifestyle.Singleton);
            container.Register(typeof(IZeroStateChangeHandler<>),
                typeof(IZeroStateChangeHandler<>).Assembly,
                Lifestyle.Scoped);

            container.RegisterDecorator(
                typeof(IDbContextConfigurer<StateStoreContext>),
                typeof(ZeroStateDbConfigurer),
                Lifestyle.Scoped);

            container.Collection.Register(
                typeof(IZeroStateInterceptor),
                new []{ typeof(IZeroStateInterceptor).Assembly },
                Lifestyle.Scoped);

            // Seeders
            container.Collection.Append<IZeroStateSeeder, NetworkProvidersSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IZeroStateSeeder, ZeroStateProjectSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IZeroStateSeeder, ZeroStateVmMetadataSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IZeroStateSeeder, ZeroStateProviderPortSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IZeroStateSeeder, ZeroStateVirtualNetworkSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IZeroStateSeeder, ZeroStateVirtualNetworkPortsSeeder>(Lifestyle.Scoped);

            container.Collection.Append<IConfigSeeder<ControllerModule>, NetworkProvidersSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IConfigSeeder<ControllerModule>, ZeroStateProjectSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IConfigSeeder<ControllerModule>, ZeroStateVmMetadataSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IConfigSeeder<ControllerModule>, ZeroStateProviderPortSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IConfigSeeder<ControllerModule>, ZeroStateVirtualNetworkSeeder>(Lifestyle.Scoped);
            container.Collection.Append<IConfigSeeder<ControllerModule>, ZeroStateVirtualNetworkPortsSeeder>(Lifestyle.Scoped);
        }

        public static void AddZeroStateService(this SimpleInjectorAddOptions options)
        {
            options.AddHostedService<ZeroStateBackgroundService2<VirtualNetworkChange>>();
            options.AddHostedService<ZeroStateBackgroundService2<ProviderPortChange>>();
            options.AddHostedService<ZeroStateBackgroundService2<ProjectChange>>();
            options.AddHostedService<ZeroStateBackgroundService2<VirtualNetworkPortChange>>();
            options.AddHostedService<ZeroStateBackgroundService2<ZeroStateCatletMetadataChange>>();
            //options.AddHostedService<ZeroStateSeedingService>();
        }
    }
}
