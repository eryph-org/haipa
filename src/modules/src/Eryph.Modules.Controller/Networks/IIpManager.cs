﻿using System;
using System.Net;
using System.Threading;
using Eryph.ConfigModel.Machine;
using Eryph.StateDb.Model;
using LanguageExt;
using LanguageExt.Common;

namespace Eryph.Modules.Controller.Networks
{
    public interface ICatletIpManager
    {
        public EitherAsync<Error, IPAddress[]> ConfigurePortIps(
            Guid tenantId, CatletNetworkPort port,
            MachineNetworkConfig[] networkConfigs, CancellationToken cancellationToken);

    }

    public interface IProviderIpManager
    {
        public EitherAsync<Error, IPAddress[]> ConfigurePortIps(
            Guid projectId, VirtualNetworkPort port,
             ProviderSubnet[] subnets, CancellationToken cancellationToken);

    }
}
