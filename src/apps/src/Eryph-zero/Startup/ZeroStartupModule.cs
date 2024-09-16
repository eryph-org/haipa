﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eryph.ModuleCore.Startup;
using Eryph.Runtime.Zero.Configuration.Clients;
using Eryph.Runtime.Zero.HttpSys;
using Eryph.Security.Cryptography;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using SimpleInjector;
using SimpleInjector.Integration.ServiceCollection;

namespace Eryph.Runtime.Zero.Startup;

/// <summary>
/// This module performs some necessary startup actions for eryph-zero.
/// We use the module with <see cref="IStartupHandler"/>s and
/// <see cref="Microsoft.Extensions.Hosting.IHostedService"/>s to avoid
/// timeouts when eryph-zero starts as a Windows service.
/// </summary>
public class ZeroStartupModule
{
    [UsedImplicitly]
    public void ConfigureContainer(IServiceProvider serviceProvider, Container container)
    {
        container.RegisterSingleton(serviceProvider.GetRequiredService<IEryphOvsPathProvider>);
        container.RegisterSingleton<ICertificateGenerator, CertificateGenerator>();
        container.RegisterSingleton<ICertificateStoreService, WindowsCertificateStoreService>();
        container.RegisterSingleton<ICryptoIOServices, WindowsCryptoIOServices>();
        container.RegisterSingleton<IRSAProvider, RSAProvider>();
        container.RegisterSingleton<ISSLEndpointManager, SSLEndpointManager>();
        container.RegisterSingleton<ISSLEndpointRegistry, WinHttpSSLEndpointRegistry>();
        container.Register<ISystemClientGenerator, SystemClientGenerator>();
    }

    [UsedImplicitly]
    public void AddSimpleInjector(SimpleInjectorAddOptions options)
    {
        options.AddLogging();
        options.AddStartupHandler<EnsureHyperVAndOvsStartupHandler>();
        options.AddStartupHandler<SystemClientStartupHandler>();
        options.AddHostedService<SslEndpointService>();
    }
}
