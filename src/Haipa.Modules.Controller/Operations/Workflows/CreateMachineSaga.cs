﻿using System;
using System.Threading.Tasks;
using Haipa.Messages.Commands;
using Haipa.Messages.Commands.OperationTasks;
using Haipa.Messages.Operations;
using Haipa.Modules.Controller.IdGenerator;
using Haipa.StateDb.Model;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Sagas;

namespace Haipa.Modules.Controller.Operations.Workflows
{
    [UsedImplicitly]
    internal class CreateMachineSaga : OperationTaskWorkflowSaga<CreateMachineCommand, CreateMachineSagaData>,
        IHandleMessages<OperationTaskStatusEvent<ValidateMachineConfigCommand>>,
        IHandleMessages<OperationTaskStatusEvent<PlaceVirtualMachineCommand>>,
        IHandleMessages<OperationTaskStatusEvent<CreateVirtualMachineCommand>>,
        IHandleMessages<OperationTaskStatusEvent<UpdateMachineCommand>>,
        IHandleMessages<OperationTaskStatusEvent<PrepareVirtualMachineImageCommand>>

    {
        private readonly IOperationTaskDispatcher _taskDispatcher;
        private readonly Id64Generator _idGenerator;
        private readonly IVirtualMachineDataService _vmDataService;

        public CreateMachineSaga(IBus bus, IOperationTaskDispatcher taskDispatcher, Id64Generator idGenerator, IVirtualMachineDataService vmDataService) : base(bus)
      {
          _taskDispatcher = taskDispatcher;
          _idGenerator = idGenerator;
          _vmDataService = vmDataService;
      }

        protected override void CorrelateMessages(ICorrelationConfig<CreateMachineSagaData> config)
        {
            base.CorrelateMessages(config);

            config.Correlate<OperationTaskStatusEvent<ValidateMachineConfigCommand>>(m => m.OperationId, d => d.OperationId);
            config.Correlate<OperationTaskStatusEvent<PlaceVirtualMachineCommand>>(m => m.OperationId, d => d.OperationId);
            config.Correlate<OperationTaskStatusEvent<CreateVirtualMachineCommand>>(m => m.OperationId, d => d.OperationId);
            config.Correlate<OperationTaskStatusEvent<PrepareVirtualMachineImageCommand>>(m => m.OperationId, d => d.OperationId);
            config.Correlate<OperationTaskStatusEvent<UpdateMachineCommand>>(m => m.OperationId, d => d.OperationId);

        }


        public override Task Initiated(CreateMachineCommand message)
        {
            Data.Config = message.Config;
            Data.State = CreateVMState.Initiated;

            return _taskDispatcher.Send(
                    new ValidateMachineConfigCommand
                    {
                        MachineId = 0,
                        Config = message.Config,
                        OperationId = Data.OperationId,
                        TaskId = Guid.NewGuid(),
                    }
                );
        }

        public Task Handle(OperationTaskStatusEvent<ValidateMachineConfigCommand> message)
        {
            if(Data.State>= CreateVMState.ConfigValidated)
                return Task.CompletedTask;

            return FailOrRun<ValidateMachineConfigCommand, ValidateMachineConfigCommand>(message, (r) =>
            {
                Data.Config = r.Config;
                Data.State = CreateVMState.ConfigValidated;
                

                return _taskDispatcher.Send(
                    new PlaceVirtualMachineCommand
                    {
                        OperationId = message.OperationId,
                        TaskId = Guid.NewGuid(),
                        Config = Data.Config,
                    });

            });
        }

        public Task Handle(OperationTaskStatusEvent<PlaceVirtualMachineCommand> message)
        {
            if (Data.State >= CreateVMState.Placed)
                return Task.CompletedTask;

            return FailOrRun<PlaceVirtualMachineCommand,PlaceVirtualMachineResult>(message, (r) =>
            {
                Data.State = CreateVMState.Placed;
                Data.AgentName = r.AgentName;

                return _taskDispatcher.Send(new PrepareVirtualMachineImageCommand
                { ImageConfig = Data.Config.Image, 
                  AgentName = r.AgentName, 
                  OperationId = message.OperationId, 
                  TaskId = Guid.NewGuid()
                });
            });

        }

        public Task Handle(OperationTaskStatusEvent<PrepareVirtualMachineImageCommand> message)
        {
            if (Data.State >= CreateVMState.ImagePrepared)
                return Task.CompletedTask;

            return FailOrRun(message, () =>
            {
                Data.State = CreateVMState.ImagePrepared;
                Data.MachineId = _idGenerator.GenerateId();

                var createMessage = new CreateVirtualMachineCommand
                    { Config = Data.Config, 
                        NewMachineId = Data.MachineId,
                        AgentName = Data.AgentName, OperationId = message.OperationId, TaskId = Guid.NewGuid() };

                return _taskDispatcher.Send(createMessage);
            });
        }

        public Task Handle(OperationTaskStatusEvent<CreateVirtualMachineCommand> message)
        {
            if (Data.State >= CreateVMState.Created)
                return Task.CompletedTask;

            return FailOrRun<CreateVirtualMachineCommand, ConvergeVirtualMachineResult>(message,async (r) =>
            {
                Data.State = CreateVMState.Created;

                var newMachine = await _vmDataService.AddNewVM(new VirtualMachine
                {
                    Id = Data.MachineId,
                    AgentName = Data.AgentName,
                    VMId = r.Inventory.VMId
                }, r.MachineMetadata);

                var convergeMessage = new UpdateMachineCommand
                { MachineId = newMachine.Id, Config = Data.Config, AgentName = Data.AgentName, OperationId = message.OperationId, TaskId = Guid.NewGuid() };

                await _taskDispatcher.Send(convergeMessage);
            });
        }

        public Task Handle(OperationTaskStatusEvent<UpdateMachineCommand> message)
        {
            if (Data.State >= CreateVMState.Updated)
                return Task.CompletedTask;

            return FailOrRun(message, () =>
            {
                Data.State = CreateVMState.Updated;
                return Complete();
            });

        }

    }
}