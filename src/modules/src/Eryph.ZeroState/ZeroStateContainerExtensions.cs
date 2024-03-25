﻿using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eryph.StateDb;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;

namespace Eryph.ZeroState
{
    public static class ZeroStateContainerExtensions
    {
        public static void UseZeroState(this Container container)
        {
            //var queue = new ZeroStateQueue<VirtualNetworkChange>();
            // TODO move file system registration somewhere else
            container.RegisterSingleton<IFileSystem, FileSystem>();
            //container.RegisterInstance<IZeroStateQueue<VirtualNetworkChange>>(queue);
            container.Register(typeof(IZeroStateQueue<>), typeof(ZeroStateQueue<>), Lifestyle.Singleton);
            container.Register(typeof(IZeroStateChangeHandler<VirtualNetworkChange>),
                typeof(ZeroStateVirtualNetworkChangeHandler),
                Lifestyle.Scoped);
            container.Register<ZeroStateVirtualNetworkInterceptor>(Lifestyle.Scoped);
            container.RegisterDecorator(typeof(IDbContextConfigurer<StateStoreContext>), typeof(ZeroStateDbConfigurer),
                Lifestyle.Scoped);
        }

        public static void AddZeroStateService(this SimpleInjectorAddOptions options)
        {
            options.AddHostedService<ZeroStateBackgroundService2<VirtualNetworkChange>>();
            options.AddHostedService<ZeroStateBackgroundService2<ProviderPortChange>>();
        }
    }
}
