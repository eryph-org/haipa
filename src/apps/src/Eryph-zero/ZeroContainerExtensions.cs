﻿using Eryph.Rebus;
using Eryph.StateDb;
using Microsoft.EntityFrameworkCore.Storage;
using Rebus.Persistence.InMem;
using Rebus.Transport.InMem;
using SimpleInjector;

namespace Eryph.Runtime.Zero
{
    internal static class ZeroContainerExtensions
    {
        public static void Bootstrap(this Container container)
        {
            container
                .UseInMemoryBus()
                .UseSqlLite();
        }

        public static Container UseInMemoryBus(this Container container)
        {
            container.RegisterInstance(new InMemNetwork(true));
            container.RegisterInstance(new InMemorySubscriberStore());
            container.Register<IRebusTransportConfigurer, InMemoryTransportConfigurer>();
            container.Register<IRebusSagasConfigurer, InMemorySagasConfigurer>();
            container.Register<IRebusSubscriptionConfigurer, InMemorySubscriptionConfigurer>();
            container.Register<IRebusTimeoutConfigurer, InMemoryTimeoutConfigurer>();
            return container;
        }

        public static Container UseSqlLite(this Container container)
        {
            container.RegisterInstance(new InMemoryDatabaseRoot());
            container.Register<IDbContextConfigurer<StateStoreContext>, SqlLiteStateStoreContextConfigurer>();
            return container;
        }
    }
}