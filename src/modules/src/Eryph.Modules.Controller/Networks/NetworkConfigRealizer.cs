﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Eryph.ConfigModel.Catlets;
using Eryph.Core.Network;
using Eryph.StateDb;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;
using Microsoft.Extensions.Logging;

namespace Eryph.Modules.Controller.Networks;

public class NetworkConfigRealizer : INetworkConfigRealizer
{
    private readonly IStateStore _stateStore;
    private readonly ILogger _log;

    public NetworkConfigRealizer(IStateStore stateStore, ILogger log)
    {
        _stateStore = stateStore;
        _log = log;
    }

    public async Task UpdateNetwork(Guid projectId, ProjectNetworksConfig config, NetworkProvidersConfiguration providerConfig)
    {
        var savedNetworks = await _stateStore
            .For<VirtualNetwork>()
            .ListAsync(new VirtualNetworkSpecs.GetForProjectConfig(projectId));

        var foundNames = new List<string>();
        foreach (var networkConfig in config.Networks)
        {
            var savedNetwork = savedNetworks.Find(x => x.Name == networkConfig.Name);
            if (savedNetwork == null)
            {
                _log.LogDebug("network {network} not found. Creating new network.", networkConfig.Name);
                var newNetwork = new VirtualNetwork
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Name = networkConfig.Name,
                    Subnets = new List<VirtualNetworkSubnet>(),
                    NetworkPorts = new List<VirtualNetworkPort>(),
                };

                savedNetworks.Add(newNetwork);
                await _stateStore.For<VirtualNetwork>().AddAsync(newNetwork);
            }

            foundNames.Add(networkConfig.Name);

        }

        var removeNetworks = savedNetworks.Where(x => !foundNames.Contains(x.Name)).ToArray();
        if (removeNetworks.Any())
            _log.LogDebug("Removing networks: {@removedNetworks}", (object)removeNetworks);

        await _stateStore.For<VirtualNetwork>().DeleteRangeAsync(removeNetworks);

        // second pass - update of new or existing records
        foreach (var networkConfig in config.Networks.DistinctBy(x => x.Name))
        {
            var savedNetwork = savedNetworks.First(x => x.Name == networkConfig.Name);

            var providerName = networkConfig.Provider?.Name ?? "default";
            var providerSubnet = networkConfig.Provider?.Subnet ?? "default";
            var providerIpPool = networkConfig.Provider?.IpPool ?? "default";

            _log.LogDebug("Updating network {network}", savedNetwork.Name);

            var networkProvider = providerConfig.NetworkProviders.First(x => x.Name == providerName);
            var isFlatNetwork = networkProvider.Type == NetworkProviderType.Flat;

            savedNetwork.NetworkProvider = providerName;
            savedNetwork.IpNetwork = networkConfig.Address;

            if (isFlatNetwork)
            {
                //remove all existing overlay network objects if provider is a flat network
                savedNetwork.RouterPort = null;
                savedNetwork.Subnets.Clear();
                foreach (var port in savedNetwork.NetworkPorts.ToSeq())
                {
                    if (port is NetworkRouterPort or ProviderRouterPort)
                        savedNetwork.NetworkPorts.Remove(port);

                    await _stateStore.LoadCollectionAsync(port, x => x.IpAssignments);
                    port.IpAssignments.Clear();
                }

                continue;
            }

            var networkAddress = IPNetwork.Parse(networkConfig.Address);

            var providerPorts = savedNetwork.NetworkPorts
                .Where(x => x is ProviderRouterPort).Cast<ProviderRouterPort>().ToArray();

            //remove all ports if more then one
            if (providerPorts.Length > 1)
            {
                _log.LogWarning("Found invalid provider port count ({count} provider ports) for {network}. Removing all provider ports.", providerPorts.Length, savedNetwork.Name);

                await _stateStore.For<ProviderRouterPort>().DeleteRangeAsync(providerPorts);
                providerPorts = Array.Empty<ProviderRouterPort>();
            }

            var providerPort = providerPorts.FirstOrDefault();
            if (providerPort != null)
            {
                if (providerPort.ProviderName != providerName ||
                    providerPort.SubnetName != providerSubnet ||
                    providerPort.PoolName != providerIpPool)
                {
                    _log.LogInformation("Network {network}: network provider settings changed.", savedNetwork.Name);

                    savedNetwork.NetworkPorts.Remove(providerPort);
                    providerPort = null;
                }
            }

            if (providerPort == null)
            {
                savedNetwork.NetworkPorts.Add(new ProviderRouterPort()
                {
                    Name = "provider",
                    SubnetName = providerSubnet,
                    PoolName = providerIpPool,
                    MacAddress = MacAddresses.FormatMacAddress(
                        MacAddresses.GenerateMacAddress(Guid.NewGuid().ToString())),
                    ProviderName = providerName,
                });
            }

            var routerPorts = savedNetwork.NetworkPorts
                .Where(x => x is NetworkRouterPort).Cast<NetworkRouterPort>().ToArray();

            //remove all ports if more then one
            if (routerPorts.Length > 1)
            {
                _log.LogWarning("Found invalid router port count ({count} router ports) for {network}. Removing all router ports.", routerPorts.Length, savedNetwork.Name);

                await _stateStore.For<NetworkRouterPort>().DeleteRangeAsync(routerPorts);
                routerPorts = Array.Empty<NetworkRouterPort>();
            }

            var routerPort = routerPorts.FirstOrDefault();


            if (routerPort != null && routerPort.Id != savedNetwork.RouterPort.Id)
            {
                savedNetwork.NetworkPorts.Remove(routerPort);
                routerPort = null;
            }

            if (routerPort != null)
            {
                await _stateStore.LoadCollectionAsync(routerPort, x => x.IpAssignments);
                var ipAssignment = routerPort.IpAssignments.FirstOrDefault();

                if (ipAssignment == null ||
                    !IPAddress.TryParse(ipAssignment.IpAddress, out var ipAddress) ||
                    !networkAddress.Contains(ipAddress))
                {
                    _log.LogInformation("Network {network}: network router ip assignment changed to {ipAddress}.", savedNetwork.Name, networkAddress.FirstUsable);

                    savedNetwork.NetworkPorts.Remove(routerPort);
                    routerPort = null;
                }
            }

            if (routerPort == null)
            {
                routerPort = new NetworkRouterPort
                {
                    MacAddress = MacAddresses.FormatMacAddress(
                        MacAddresses.GenerateMacAddress(Guid.NewGuid().ToString())),
                    IpAssignments = new List<IpAssignment>(new[]
                    {
                        new IpAssignment
                        {
                            IpAddress = networkAddress.FirstUsable.ToString(),
                        }
                    }),
                    Name = "default",
                    RoutedNetworkId = savedNetwork.Id,
                    NetworkId = savedNetwork.Id,
                };

                savedNetwork.RouterPort = routerPort;
                savedNetwork.NetworkPorts.Add(routerPort);
            }


            foundNames.Clear();

            foreach (var subnetConfig in networkConfig.Subnets)
            {
                foundNames.Add(subnetConfig.Name);

                var savedSubnet = savedNetwork.Subnets.FirstOrDefault(x => x.Name ==
                                                                           subnetConfig.Name);
                if (savedSubnet == null)
                {
                    _log.LogDebug("subnet {network}/{subnet} not found. Creating new subnet.", networkConfig.Name, subnetConfig.Name);

                    savedNetwork.Subnets.Add(new VirtualNetworkSubnet
                    {
                        Name = subnetConfig.Name,
                        IpPools = new List<IpPool>(),
                        NetworkId = savedNetwork.Id
                    });
                }
            }

            var removeSubnets = savedNetwork.Subnets
                .Where(x => !foundNames.Contains(x.Name)).ToArray();

            if (removeSubnets.Any())
                _log.LogDebug("Removing subnets: {@removeSubnets}", (object)removeSubnets);

            await _stateStore.For<VirtualNetworkSubnet>().DeleteRangeAsync(removeSubnets);

            foreach (var subnetConfig in networkConfig.Subnets.DistinctBy(x => x.Name))
            {
                var savedSubnet = savedNetwork.Subnets.First(x => x.Name == subnetConfig.Name);

                _log.LogDebug("Updating subnet {network}/{subnet}", savedNetwork.Name, savedSubnet.Name);


                savedSubnet.DhcpLeaseTime = 3600;
                savedSubnet.MTU = subnetConfig.Mtu;
                savedSubnet.DnsServersV4 = subnetConfig.DnsServers != null
                    ? string.Join(',', subnetConfig.DnsServers)
                    : null;
                savedSubnet.IpNetwork = subnetConfig.Address ?? networkConfig.Address;


                foundNames.Clear();

                foreach (var ipPoolConfig in subnetConfig.IpPools.DistinctBy(x => x.Name))
                {
                    foundNames.Add(ipPoolConfig.Name);

                    var savedIpPool = savedSubnet.IpPools.FirstOrDefault(x => x.Name == ipPoolConfig.Name);

                    // ip pool recreation - validation has ensured that it is no longer in use
                    if (savedIpPool != null && savedIpPool.IpNetwork != savedSubnet.IpNetwork)
                    {
                        savedSubnet.IpPools.Remove(savedIpPool);
                        savedIpPool = null;
                    }

                    // change of ip pool is allowed if validation has passed (enough space in pool or unused)
                    if (savedIpPool != null)
                    {
                        _log.LogDebug("Updating ip pool {network}/{subnet}/{pool}", savedNetwork.Name,
                            savedSubnet.Name, savedIpPool.Name);

                        savedIpPool.IpNetwork = savedSubnet.IpNetwork;
                        savedIpPool.FirstIp = ipPoolConfig.FirstIp;
                        savedIpPool.LastIp = ipPoolConfig.LastIp;
                    }

                    if (savedIpPool == null)
                    {
                        _log.LogDebug("creating new ip pool {network}/{subnet}/{pool}", savedNetwork.Name,
                            savedSubnet.Name, ipPoolConfig.Name);

                        savedSubnet.IpPools.Add(new IpPool
                        {
                            Name = ipPoolConfig.Name,
                            FirstIp = ipPoolConfig.FirstIp,
                            LastIp = ipPoolConfig.LastIp,
                            IpNetwork = savedSubnet.IpNetwork
                        });
                    }
                }

                var removeIpPools = savedSubnet.IpPools.Where(x => !foundNames.Contains(x.Name));
                await _stateStore.For<IpPool>().DeleteRangeAsync(removeIpPools);
            }
        }

    }

}