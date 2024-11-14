﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Dbosoft.Rebus.Operations;
using Eryph.ConfigModel;
using Eryph.ConfigModel.Catlets;
using Eryph.Core;
using Eryph.Core.Network;
using Eryph.Messages.Resources.Catlets.Commands;
using Eryph.Resources.Machines;
using Eryph.StateDb;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;
using Rebus.Handlers;

using static LanguageExt.Prelude;

namespace Eryph.Modules.Controller.Networks;

[UsedImplicitly]
public class UpdateCatletNetworksCommandHandler(
    ITaskMessaging messaging,
    ICatletIpManager ipManager,
    IStateStore stateStore,
    INetworkProviderManager providerManager,
    IProviderIpManager providerIpManager)
    : IHandleMessages<OperationTask<UpdateCatletNetworksCommand>>
{
    public async Task Handle(OperationTask<UpdateCatletNetworksCommand> message)
    {
        await messaging.ProgressMessage(message, "Updating catlet network settings");
        await UpdateNetworks(message.Command)
            .Map(settings => new UpdateCatletNetworksCommandResponse
            {
                NetworkSettings = settings.ToArray()
            })
            .FailOrComplete(messaging, message);
    }

    public EitherAsync<Error, Seq<MachineNetworkSettings>> UpdateNetworks(
        UpdateCatletNetworksCommand command) =>
        from _ in RightAsync<Error, Unit>(unit)
        let usedPortNames = command.Config.Networks
            .ToSeq()
            .Map(cfg => GetPortName(command.CatletId, cfg.AdapterName))
        from unusedPorts in stateStore.For<CatletNetworkPort>().IO.ListAsync(
            new CatletNetworkPortSpecs.GetUnused(command.CatletMetadataId, usedPortNames))
        from __ in unusedPorts.Map(RemovePort)
            .SequenceSerial()
        from settings in command.Config.Networks
            .ToSeq()
            .Map(cfg => UpdateNetwork(command.CatletMetadataId, command, cfg))
            .SequenceSerial()
        select settings;

    private EitherAsync<Error, MachineNetworkSettings> UpdateNetwork(
        Guid catletMetadataId,
        UpdateCatletNetworksCommand command,
        CatletNetworkConfig networkConfig) =>
        from environmentName in Optional(command.Config.Environment)
            .Map(EnvironmentName.NewEither)
            .Sequence().ToAsync()
            .Map(n => n.IfNone(EnvironmentName.New(EryphConstants.DefaultEnvironmentName)))
        from networkName in Optional(networkConfig.Name)
            .Map(EryphNetworkName.NewEither)
            .Sequence().ToAsync()
            .Map(n => n.IfNone(EryphNetworkName.New(EryphConstants.DefaultNetworkName)))
        
        from network in stateStore.For<VirtualNetwork>().IO.GetBySpecAsync(
            new VirtualNetworkSpecs.GetByName(command.ProjectId, networkName.Value, environmentName.Value))
        // It is optional to have an environment specific network. Therefore,
        // we fall back to the network in the default environment.
        from validNetwork in network.IsNone && environmentName != EnvironmentName.New(EryphConstants.DefaultEnvironmentName)
            ? stateStore.For<VirtualNetwork>().IO.GetBySpecAsync(
                    new VirtualNetworkSpecs.GetByName(command.ProjectId, networkName.Value, EryphConstants.DefaultEnvironmentName))
                .Bind(o => o.ToEitherAsync(
                    Error.New($"Network '{networkName}' not found in environment '{environmentName}' and default environment.")))
            : network.ToEitherAsync(Error.New($"Network '{networkName}' not found in environment '{environmentName}'."))

        from networkProviders in providerManager.GetCurrentConfiguration()
        from networkProvider in networkProviders.NetworkProviders
            .Find(x => x.Name == validNetwork.NetworkProvider)
            .ToEither(Error.New($"Network provider {validNetwork.NetworkProvider} not found."))
            .ToAsync()
        let isFlatNetwork = networkProvider.Type == NetworkProviderType.Flat

        let fixedMacAddress = command.Config.NetworkAdapters
            .ToSeq()
            .Find(x => x.Name == networkConfig.AdapterName)
            .Bind(x => Optional(x.MacAddress))
        let hostname = Optional(command.Config.Hostname).Filter(notEmpty)
            | Optional(command.Config.Name).Filter(notEmpty)

        from networkPort in AddOrUpdateAdapterPort(
            validNetwork, command.CatletId, catletMetadataId,
            networkConfig.AdapterName, hostname.IfNoneUnsafe((string?)null), fixedMacAddress)
        
        from ips in isFlatNetwork
            ? from existingAssignments in stateStore.For<IpAssignment>().IO.ListAsync(
                  new IPAssignmentSpecs.GetByPort(networkPort.Id))
              from _ in existingAssignments.Match(
                  Empty: () => RightAsync<Error, Unit>(unit),
                  Seq: a => stateStore.For<IpAssignment>().IO.DeleteRangeAsync(a))
              select Seq<IPAddress>()
            : ipManager.ConfigurePortIps(validNetwork, networkPort, networkConfig)

        from floatingIps in isFlatNetwork
            ? from _ in Optional(networkPort.FloatingPort)
                  .Map(fp => stateStore.For<FloatingNetworkPort>().IO.DeleteAsync(fp))
                  .Sequence()
              select Option<Seq<IPAddress>>.None
            : from providerPort in stateStore.For<ProviderRouterPort>().IO.GetBySpecAsync(
                  new ProviderRouterPortSpecs.GetByNetworkId(validNetwork.Id))
              from validProviderPort in providerPort.ToEitherAsync(
                  Error.New($"The overlay network '{validNetwork.Name}' has no provider port."))
              let providerSubnetName = validProviderPort.SubnetName
              let providerPoolName = validProviderPort.PoolName
              from fp in UpdateFloatingPort(networkPort, networkProvider.Name, providerSubnetName, providerPoolName)
              from ips in providerIpManager.ConfigureFloatingPortIps(networkProvider.Name, fp)
              select Some(ips)

        select new MachineNetworkSettings
        {
            NetworkProviderName = validNetwork.NetworkProvider,
            NetworkName = networkConfig.Name,
            AdapterName = networkConfig.AdapterName,
            PortName = networkPort.Name,
            MacAddress = networkPort.MacAddress,
            AddressesV4 = ips.Filter(x => x.AddressFamily == AddressFamily.InterNetwork)
                .Map(ip => ip.ToString())
                .ToList(),
            FloatingAddressV4 = floatingIps.ToSeq().Flatten()
                .Find(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                .Map(ip => ip.ToString())
                .IfNoneUnsafe((string?)null),
            AddressesV6 = ips.Filter(x => x.AddressFamily == AddressFamily.InterNetworkV6)
                .Map(ip => ip.ToString())
                .ToList(),
            FloatingAddressV6 = floatingIps.ToSeq().Flatten()
                .Find(ip => ip.AddressFamily == AddressFamily.InterNetworkV6)
                .Map(ip => ip.ToString())
                .IfNoneUnsafe((string?)null),
        };

    private EitherAsync<Error, CatletNetworkPort> AddOrUpdateAdapterPort(
        VirtualNetwork network,
        Guid catletId,
        Guid catletMetadataId,
        string adapterName,
        string addressName,
        Option<string> fixedMacAddress) =>
        from _ in RightAsync<Error, Unit>(unit)
        let portName = GetPortName(catletId, adapterName)
        let macAddress = fixedMacAddress
            .Filter(notEmpty)
            .IfNone(() => MacAddresses.GenerateMacAddress(portName))
        from existingPort in stateStore.For<CatletNetworkPort>().IO.GetBySpecAsync(
            new CatletNetworkPortSpecs.GetByCatletMetadataIdAndName(catletMetadataId, portName))
        from updatedPort in existingPort.Match(
            Some: p =>
                from _ in RightAsync<Error, Unit>(unit)
                let __ =  fun(() =>
                {
                    p.AddressName = addressName;
                    p.MacAddress = MacAddresses.FormatMacAddress(macAddress);
                    p.Network = network;
                })()
                select p,
            None: () =>
                from _ in RightAsync<Error, Unit>(unit)
                let newPort = new CatletNetworkPort
                {
                    Id = Guid.NewGuid(),
                    CatletMetadataId = catletMetadataId,
                    Name = portName,
                    MacAddress = MacAddresses.FormatMacAddress(macAddress),
                    Network = network,
                    AddressName = addressName,
                    IpAssignments = [],
                }
                from addedPort in stateStore.For<CatletNetworkPort>().IO.AddAsync(newPort)
                select addedPort)
        select updatedPort;

    

    private EitherAsync<Error, FloatingNetworkPort> UpdateFloatingPort(
        CatletNetworkPort adapterPort,
        string providerName,
        string providerSubnetName,
        string providerPoolName) =>
        from _ in RightAsync<Error, Unit>(unit)
        let existingFloatingPort = Optional(adapterPort.FloatingPort)
        from validFloatingPort in existingFloatingPort
            .Filter(fp => fp.ProviderName == providerName
                          && fp.SubnetName == providerSubnetName
                          && fp.PoolName == providerPoolName)
            .Map(RightAsync<Error, FloatingNetworkPort>)
            .IfNone(() =>
                from _ in existingFloatingPort
                    .Map(fp => stateStore.For<FloatingNetworkPort>().IO.DeleteAsync(fp))
                    .Sequence()
                let fp = new FloatingNetworkPort
                {
                    Id = Guid.NewGuid(),
                    Name = adapterPort.Name,
                    ProviderName = providerName,
                    SubnetName = providerSubnetName,
                    PoolName = providerPoolName,
                    MacAddress = MacAddresses.FormatMacAddress(MacAddresses.GenerateMacAddress(Guid.NewGuid().ToString()))
                }
                from __ in stateStore.For<FloatingNetworkPort>().IO.AddAsync(fp)
                let ___ = fun(() => adapterPort.FloatingPort = fp)()
                select fp)
        select validFloatingPort;

    private EitherAsync<Error, Unit> RemovePort(CatletNetworkPort port) =>
        from _ in Optional(port.FloatingPort)
            .Map(fp => stateStore.For<FloatingNetworkPort>().IO.DeleteAsync(fp))
            .Sequence()
        from __ in stateStore.For<CatletNetworkPort>().IO.DeleteAsync(port)
        select unit;

    private static string GetPortName(Guid catletId, string adapterName) =>
        $"{catletId}_{adapterName}";
}
