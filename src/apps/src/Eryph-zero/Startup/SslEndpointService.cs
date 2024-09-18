﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eryph.ModuleCore;
using Eryph.Runtime.Zero.Configuration;
using Eryph.Runtime.Zero.HttpSys;
using Microsoft.Extensions.Hosting;

namespace Eryph.Runtime.Zero.Startup;

internal sealed class SslEndpointService(
    IEndpointResolver endpointResolver,
    ISSLEndpointManager sslEndpointManager)
    : IHostedService, IDisposable
{
    private SSLEndpointContext? _sslEndpointContext;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var baseUrl = endpointResolver.GetEndpoint("base");

        _sslEndpointContext = await sslEndpointManager.EnableSslEndpoint(new SSLOptions(
            "eryph-zero CA",
            Network.FQDN,
            DateTime.UtcNow.AddDays(-1),
            365 * 5,
            ZeroConfig.GetPrivateConfigPath(),
            "eryphCA",
            Guid.Parse("9412ee86-c21b-4eb8-bd89-f650fbf44931"),
            baseUrl));
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _sslEndpointContext?.Dispose();
    }
}