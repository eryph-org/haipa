﻿using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dbosoft.Rebus.Operations;
using Eryph.Core;
using Eryph.Core.Network;
using Eryph.Messages;
using Eryph.Messages.Projects;
using Eryph.StateDb;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;
using JetBrains.Annotations;
using LanguageExt;
using Rebus.Handlers;

namespace Eryph.Modules.Controller.Projects
{
    [UsedImplicitly]
    internal class CreateProjectCommandHandler : IHandleMessages<OperationTask<CreateProjectCommand>>
    {
        private readonly IStateStore _stateStore;
        private readonly ITaskMessaging _messaging;
        private readonly INetworkProviderManager _networkProviderManager;
        
        public CreateProjectCommandHandler(IStateStore stateStore, INetworkProviderManager networkProviderManager, 
            ITaskMessaging messaging)
        {
            _stateStore = stateStore;
            _networkProviderManager = networkProviderManager;
            _messaging = messaging;
        }

        public async Task Handle(OperationTask<CreateProjectCommand> message)
        {
            var stoppingToken = new CancellationTokenSource(10000);

            var name = message.Command.ProjectName.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name) || name.StartsWith("p_") 
                                                || name == "default")
            {
                await _messaging.FailTask(message,
                    $"Project name '{name}' is a reserved name.");
                return;
            }

            var existingProject = await _stateStore.For<Project>().GetBySpecAsync(
                new ProjectSpecs.GetByName(message.Command.TenantId, name), stoppingToken.Token);

            if (existingProject != null)
            {
                await _messaging.FailTask(message,
                    $"Project with name '{name}' already exists in tenant. Project names have to be unique within a tenant.");
                return;
            }



            var project = await _stateStore.For<Project>().AddAsync(
                new Project
                {
                    Id = message.Command.CorrelationId, Name = name,
                    TenantId = message.Command.TenantId
                }, stoppingToken.Token);

            await _messaging.ProgressMessage(message, $"Creating project '{name}'");

            if (!string.IsNullOrWhiteSpace(message.Command.IdentityId))
            {
                var roleAssignment = new ProjectRoleAssignment()
                {
                    Id = Guid.NewGuid(),
                    IdentityId = message.Command.IdentityId,
                    ProjectId = project.Id,
                    RoleId = EryphConstants.BuildInRoles.Owner
                };

                await _stateStore.For<ProjectRoleAssignment>().AddAsync(
                    roleAssignment, stoppingToken.Token);
            }

            if (!message.Command.NoDefaultNetwork)
            {
                _ = await _networkProviderManager.GetCurrentConfiguration().MapAsync(
                    async providerConfig =>
                    {
                        var defaultConfig = providerConfig.NetworkProviders.FirstOrDefault(x => x.Name == "default");
                        if (defaultConfig == null || defaultConfig.Type == NetworkProviderType.Flat)
                        {
                            var reason = defaultConfig == null 
                                ? "Default network provider not found. " 
                                : "Default network provider is a flat network provider. ";

                            await _messaging.ProgressMessage(message, 
                                $"{reason}No default network will be created for project '{name}'.");

                            return Unit.Default;
                        }

                        await _messaging.ProgressMessage(message, $"Creating default network for project '{name}'.");

                        var networkId = Guid.NewGuid();
                        var routerPort = new NetworkRouterPort
                        {
                            Id = Guid.NewGuid(),
                            MacAddress = MacAddresses.FormatMacAddress(
                                MacAddresses.GenerateMacAddress(Guid.NewGuid().ToString())),
                            IpAssignments = new List<IpAssignment>(new[]
                            {
                                new IpAssignment
                                {
                                    Id = Guid.NewGuid(),
                                    IpAddress = "10.0.0.1",
                                }
                            }),
                            Name = "default",
                            RoutedNetworkId = networkId,
                            NetworkId = networkId,
                        };

                        var network = new VirtualNetwork
                        {
                            Id = networkId,
                            Name = "default",
                            Environment = "default",
                            ProjectId = project.Id,
                            IpNetwork = "10.0.0.0/20",
                            NetworkProvider = "default",
                            RouterPort = routerPort,
                            NetworkPorts = new List<VirtualNetworkPort>
                            {
                                routerPort,
                                new ProviderRouterPort()
                                {
                                    Name = "provider",
                                    Id = Guid.NewGuid(),
                                    ProviderName = "default",
                                    SubnetName = "default",
                                    PoolName = "default",
                                    MacAddress = MacAddresses.FormatMacAddress(
                                        MacAddresses.GenerateMacAddress(Guid.NewGuid().ToString())),
                                }

                            },
                            Subnets = new List<VirtualNetworkSubnet>(new[]
                            {
                                new VirtualNetworkSubnet
                                {
                                    Id = Guid.NewGuid(),
                                    IpNetwork = "10.0.0.0/20",
                                    Name = "default",
                                    DhcpLeaseTime = 3600,
                                    MTU = 1400,
                                    DnsServersV4 = "9.9.9.9,8.8.8.8",
                                    IpPools = new List<IpPool>(new[]
                                    {
                                        new IpPool
                                        {
                                            Id = Guid.NewGuid(),
                                            Name = "default",
                                            IpNetwork = "10.0.0.0/20",
                                            Counter = 0,
                                            FirstIp = "10.0.0.100",
                                            LastIp = "10.0.2.240"
                                        }
                                    })
                                }
                            })
                        };

                        await _stateStore.For<VirtualNetwork>().AddAsync(network, stoppingToken.Token);
                        return Unit.Default;
                    }).IfLeft(l => l.Throw());
            }

            await _messaging.CompleteTask(message, new ProjectReference
            {
                ProjectId = message.Command.CorrelationId,
                ProjectName = name
            });
        }
    }
}
