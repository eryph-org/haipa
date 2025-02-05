﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dbosoft.Rebus.Operations.Events;
using Dbosoft.Rebus.Operations.Workflow;
using Eryph.Core.Genetics;
using Eryph.Messages.Genes.Commands;
using Eryph.Messages.Resources.Catlets.Commands;
using Eryph.Messages.Resources.Networks.Commands;
using Eryph.ModuleCore;
using Eryph.Modules.Controller.DataServices;
using Eryph.StateDb.Model;
using IdGen;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Sagas;

using CatletMetadata = Eryph.Resources.Machines.CatletMetadata;

namespace Eryph.Modules.Controller.Compute;

[UsedImplicitly]
internal class UpdateCatletSaga(
    IWorkflow workflow,
    IBus bus,
    IIdGenerator<long> idGenerator,
    IVirtualMachineDataService vmDataService,
    IVirtualMachineMetadataService metadataService)
    : OperationTaskWorkflowSaga<UpdateCatletCommand, EryphSagaData<UpdateCatletSagaData>>(workflow),
        IHandleMessages<OperationTaskStatusEvent<PrepareCatletConfigCommand>>,
        IHandleMessages<OperationTaskStatusEvent<PrepareGeneCommand>>,
        IHandleMessages<OperationTaskStatusEvent<UpdateCatletVMCommand>>,
        IHandleMessages<OperationTaskStatusEvent<UpdateCatletConfigDriveCommand>>,
        IHandleMessages<OperationTaskStatusEvent<UpdateCatletNetworksCommand>>,
        IHandleMessages<OperationTaskStatusEvent<UpdateNetworksCommand>>,
        IHandleMessages<OperationTaskStatusEvent<SyncVmNetworkPortsCommand>>
{
    protected override async Task Initiated(UpdateCatletCommand message)
    {
        Data.Data.State = UpdateVMState.Initiated;
        Data.Data.BredConfig = message.BredConfig;
        Data.Data.ResolvedGenes = message.ResolvedGenes;
        Data.Data.Config = message.Config;
        Data.Data.CatletId = message.CatletId;

        if (Data.Data.CatletId == Guid.Empty)
        {
            await Fail("Catlet cannot be updated because the catlet Id is missing.");
            return;
        }

        var machineInfo = await vmDataService.GetVM(Data.Data.CatletId)
            .Map(m => m.IfNoneUnsafe((Catlet?)null));
        if (machineInfo is null)
        {
            await Fail($"Catlet cannot be updated because the catlet {Data.Data.CatletId} does not exist.");
            return;
        }

        Data.Data.ProjectId = machineInfo.ProjectId;
        Data.Data.AgentName = machineInfo.AgentName;
        Data.Data.TenantId = machineInfo.Project.TenantId;

        if (Data.Data.ProjectId == Guid.Empty)
        {
            await Fail($"Catlet {Data.Data.CatletId} is not assigned to any project.");
            return;
        }

        var metadata = await metadataService.GetMetadata(machineInfo.MetadataId)
            .Map(m => m.IfNoneUnsafe((CatletMetadata?)null));
        if (metadata is null)
        {
            await Fail($"Catlet cannot be updated because the metadata for catlet {Data.Data.CatletId} does not exist.");
            return;
        }

        Data.Data.Architecture = Architecture.New(metadata.Architecture);

        if (Data.Data.BredConfig is not null && Data.Data.ResolvedGenes is not null)
        {
            // The catlet has already been bred. This happens when the update
            // saga is initiated by the create saga. We can skip directly to
            // the gene preparation step.
            await StartPrepareGenes();
            return;
        }

        await StartNewTask(new PrepareCatletConfigCommand
        {
            CatletId = Data.Data.CatletId,
            Config = Data.Data.Config,
        });
    }

    public Task Handle(OperationTaskStatusEvent<PrepareCatletConfigCommand> message)
    {
        if (Data.Data.State >= UpdateVMState.ConfigPrepared)
            return Task.CompletedTask;

        return FailOrRun(message, async (PrepareCatletConfigCommandResponse response) =>
        {
            Data.Data.State = UpdateVMState.ConfigPrepared;
            Data.Data.Config = response.Config;
            Data.Data.BredConfig = response.BredConfig;
            Data.Data.ResolvedGenes = response.ResolvedGenes;

            await StartPrepareGenes();
        });
    }

    public Task Handle(OperationTaskStatusEvent<PrepareGeneCommand> message)
    {
        if (Data.Data.State >= UpdateVMState.GenesPrepared)
            return Task.CompletedTask;

        return FailOrRun(message, async (PrepareGeneResponse response) =>
        {
            Data.Data.PendingGenes = Data.Data.PendingGenes
                .Except([response.RequestedGene])
                .ToList();

            await bus.SendLocal(new UpdateGenesInventoryCommand
            {
                AgentName = Data.Data.AgentName,
                Inventory = [response.Inventory],
                Timestamp = response.Timestamp,
            });

            if (Data.Data.PendingGenes.Count > 0)
                return;

            await StartUpdateCatlet();
        });
    }

    private async Task StartUpdateCatlet()
    {
        Data.Data.State = UpdateVMState.GenesPrepared;

        var metadata = await GetCatletMetadata(Data.Data.CatletId);
        if (metadata.IsNone)
        {
            await Fail($"The metadata for catlet {Data.Data.CatletId} was not found.");
            return;
        }

        await StartNewTask(new UpdateCatletNetworksCommand
        {
            CatletId = Data.Data.CatletId,
            CatletMetadataId = metadata.ValueUnsafe().Metadata.Id,
            Config = Data.Data.BredConfig,
            ProjectId = Data.Data.ProjectId
        });
    }

    public Task Handle(OperationTaskStatusEvent<UpdateCatletNetworksCommand> message)
    {
        return FailOrRun(message, async (UpdateCatletNetworksCommandResponse r) =>
        {
            var metadata = await GetCatletMetadata(Data.Data.CatletId);
            if (metadata.IsNone)
            {
                await Fail($"The metadata for catlet {Data.Data.CatletId} was not found.");
                return;
            }

            await StartNewTask(new UpdateCatletVMCommand
            {
                CatletId = Data.Data.CatletId,
                VMId = metadata.ValueUnsafe().Catlet.VMId,
                Config = Data.Data.BredConfig,
                AgentName = Data.Data.AgentName,
                NewStorageId = idGenerator.CreateId(),
                MachineMetadata = metadata.ValueUnsafe().Metadata,
                MachineNetworkSettings = r.NetworkSettings,
                ResolvedGenes = Data.Data.ResolvedGenes,
            });
        });
    }

    public Task Handle(OperationTaskStatusEvent<UpdateCatletVMCommand> message)
    {
        if (Data.Data.State >= UpdateVMState.VMUpdated)
            return Task.CompletedTask;

        return FailOrRun(message, async (ConvergeCatletResult response) =>
        {
            Data.Data.State = UpdateVMState.VMUpdated;


            //TODO: replace this with operation call
            await bus.SendLocal(new UpdateInventoryCommand
            {
                AgentName = Data.Data.AgentName,
                Inventory = response.Inventory,
                Timestamp = response.Timestamp,
            });

            var metadata = await GetCatletMetadata(Data.Data.CatletId);
            if (metadata.IsNone)
            {
                await Fail($"The metadata for catlet {Data.Data.CatletId} was not found.");
                return;
            }

            await StartNewTask(new UpdateCatletConfigDriveCommand
            {
                Config = Data.Data.BredConfig,
                VMId = response.Inventory.VMId,
                CatletId = Data.Data.CatletId,
                MachineMetadata = metadata.ValueUnsafe().Metadata,
                ResolvedGenes = Data.Data.ResolvedGenes,
            });
        });
    }

    public Task Handle(OperationTaskStatusEvent<UpdateCatletConfigDriveCommand> message)
    {
        if (Data.Data.State >= UpdateVMState.ConfigDriveUpdated)
            return Task.CompletedTask;

        return FailOrRun(message, async () =>
        {
            Data.Data.State = UpdateVMState.ConfigDriveUpdated;

            await StartNewTask(new UpdateNetworksCommand
            {
                Projects = [Data.Data.ProjectId]
            });
        });
    }

    public Task Handle(OperationTaskStatusEvent<UpdateNetworksCommand> message)
    {
        if (Data.Data.State >= UpdateVMState.NetworksUpdated)
            return Task.CompletedTask;

        return FailOrRun(message, async () =>
        {
            Data.Data.State = UpdateVMState.NetworksUpdated;

            var metadata = await GetCatletMetadata(Data.Data.CatletId);
            if (metadata.IsNone)
            {
                await Fail($"The metadata for catlet {Data.Data.CatletId} was not found.");
                return;
            }

            await StartNewTask(new SyncVmNetworkPortsCommand
            {
                CatletId = Data.Data.CatletId,
                VMId = metadata.ValueUnsafe().Catlet.VMId,
            });
        });
    }

    public Task Handle(OperationTaskStatusEvent<SyncVmNetworkPortsCommand> message)
    {
        return FailOrRun(message, () => Complete());
    }

    protected override void CorrelateMessages(ICorrelationConfig<EryphSagaData<UpdateCatletSagaData>> config)
    {
        base.CorrelateMessages(config);

        config.Correlate<OperationTaskStatusEvent<PrepareCatletConfigCommand>>(
            m => m.InitiatingTaskId, d => d.SagaTaskId);
        config.Correlate<OperationTaskStatusEvent<PrepareGeneCommand>>(
            m => m.InitiatingTaskId, d => d.SagaTaskId);
        config.Correlate<OperationTaskStatusEvent<UpdateCatletNetworksCommand>>(
            m => m.InitiatingTaskId, d => d.SagaTaskId);
        config.Correlate<OperationTaskStatusEvent<UpdateCatletVMCommand>>(
            m => m.InitiatingTaskId, d => d.SagaTaskId);
        config.Correlate<OperationTaskStatusEvent<UpdateCatletConfigDriveCommand>>(
            m => m.InitiatingTaskId, d => d.SagaTaskId);
        config.Correlate<OperationTaskStatusEvent<UpdateNetworksCommand>>(
            m => m.InitiatingTaskId, d => d.SagaTaskId);
        config.Correlate<OperationTaskStatusEvent<SyncVmNetworkPortsCommand>>(
            m => m.InitiatingTaskId, d => d.SagaTaskId);
    }

    private async Task StartPrepareGenes()
    {
        Data.Data.State = UpdateVMState.ConfigPrepared;

        if (Data.Data.ResolvedGenes!.Count == 0)
        {
            // no images required - go directly to catlet update
            Data.Data.State = UpdateVMState.GenesPrepared;
            Data.Data.PendingGenes = [];
            await StartUpdateCatlet();
            return;
        }

        Data.Data.PendingGenes = Data.Data.ResolvedGenes;
        var commands = Data.Data.ResolvedGenes.Map(id => new PrepareGeneCommand
        {
            AgentName = Data.Data.AgentName,
            Gene = id,
        });

        foreach (var command in commands)
        {
            await StartNewTask(command);
        }
    }

    private Task<Option<(Catlet Catlet, CatletMetadata Metadata)>> GetCatletMetadata(Guid catletId) =>
        from catlet in vmDataService.GetVM(catletId)
        from metadata in metadataService.GetMetadata(catlet.MetadataId)
        select (catlet, metadata);
}
