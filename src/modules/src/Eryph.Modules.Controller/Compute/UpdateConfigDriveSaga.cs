﻿using System.Threading.Tasks;
using Dbosoft.Rebus.Operations.Events;
using Dbosoft.Rebus.Operations.Workflow;
using Eryph.ConfigModel.Catlets;
using Eryph.Core;
using Eryph.Core.Genetics;
using Eryph.Messages.Resources.Catlets.Commands;
using Eryph.Modules.Controller.DataServices;
using Eryph.StateDb.Model;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Rebus.Handlers;
using Rebus.Sagas;
using CatletMetadata = Eryph.Resources.Machines.CatletMetadata;

namespace Eryph.Modules.Controller.Compute
{
    [UsedImplicitly]
    internal class UpdateConfigDriveSaga :
        OperationTaskWorkflowSaga<UpdateConfigDriveCommand, UpdateConfigDriveSagaData>,
        IHandleMessages<OperationTaskStatusEvent<UpdateCatletConfigDriveCommand>>
    {
        private readonly IVirtualMachineDataService _vmDataService;
        private readonly IVirtualMachineMetadataService _metadataService;
        public UpdateConfigDriveSaga(IWorkflow workflow, IVirtualMachineDataService vmDataService, IVirtualMachineMetadataService metadataService) 
            : base(workflow)
        {
            _vmDataService = vmDataService;
            _metadataService = metadataService;
        }

        protected override async Task Initiated(UpdateConfigDriveCommand message)
        {
            var machineInfo = await _vmDataService.GetVM(message.CatletId)
                .Map(m => m.IfNoneUnsafe((Catlet?)null));
            if (machineInfo is null)
            {
                await Fail($"Catlet config drive cannot be updated because the catlet {message.CatletId} does not exist.");
                return;
            }

            var metadata = await _metadataService.GetMetadata(machineInfo.MetadataId)
                .Map(m => m.IfNoneUnsafe((CatletMetadata?)null));
            if (metadata is null)
            {
                await Fail($"Catlet config drive cannot be updated because the metadata for catlet {message.CatletId} does not exist.");
                return;
            }

            // This saga only updates the config drive which is attached to catlet without
            // updating the catlet itself. We breed a fake catlet config which can be passed
            // to the UpdateCatletConfigDriveCommand to update the config drive.
            var config = new CatletConfig()
            {
                Name = machineInfo.Name,
                // TODO what about hostname?
                Fodder = metadata.Fodder,
                Variables = metadata.Variables,
            };

            var breedingResult = CatletBreeding.Breed(metadata.ParentConfig, config);
            if (breedingResult.IsLeft)
            {
                await Fail(ErrorUtils.PrintError(Error.New($"Could not breed config for catlet {message.CatletId}.",
                    Error.Many(breedingResult.LeftToSeq()))));
                return;
            }

            await StartNewTask(new UpdateCatletConfigDriveCommand
            {
                Config = breedingResult.ValueUnsafe(),
                VMId = machineInfo.VMId,
                CatletId = machineInfo.Id,
                MachineMetadata = metadata
            });
        }

        public Task Handle(OperationTaskStatusEvent<UpdateCatletConfigDriveCommand> message)
        {
            return FailOrRun(message, () => Complete());
        }

        protected override void CorrelateMessages(ICorrelationConfig<UpdateConfigDriveSagaData> config)
        {
            base.CorrelateMessages(config);
            config.Correlate<OperationTaskStatusEvent<UpdateCatletConfigDriveCommand>>(m => m.InitiatingTaskId, m => m.SagaTaskId);
        }
    }
}