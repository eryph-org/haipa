﻿using System;
using System.Net;
using System.Threading;
using Eryph.ConfigModel.Catlets;
using Eryph.Core.Network;
using Eryph.StateDb.Model;
using LanguageExt;
using LanguageExt.Common;

namespace Eryph.Modules.Controller.Networks
{
    public interface ICatletIpManager
    {
        public EitherAsync<Error, IPAddress[]> ConfigurePortIps(
            Guid tenantId, CatletNetworkPort port,
            CatletNetworkConfig[] networkConfigs, CancellationToken cancellationToken);

    }

    public interface IProviderIpManager
    {
        public EitherAsync<Error, IPAddress[]> ConfigureFloatingPortIps(
            NetworkProvider provider, FloatingNetworkPort port, CancellationToken cancellationToken);

    }
}
