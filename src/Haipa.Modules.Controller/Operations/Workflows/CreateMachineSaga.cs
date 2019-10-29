﻿using System;
using System.Threading.Tasks;
using Haipa.Messages.Commands.OperationTasks;
using Haipa.Messages.Operations;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Sagas;

namespace Haipa.Modules.Controller.Operations.Workflows
{
    [UsedImplicitly]
    internal class CreateMachineSaga : OperationTaskWorkflowSaga<CreateMachineCommand, CreateMachineSagaData>,
        IHandleMessages<OperationTaskStatusEvent<PlaceVirtualMachineCommand>>,
        IHandleMessages<OperationTaskStatusEvent<ConvergeVirtualMachineCommand>>,
        IHandleMessages<OperationTaskStatusEvent<PrepareVirtualMachineImageCommand>>

    {
        private readonly IOperationTaskDispatcher _taskDispatcher;

        public CreateMachineSaga(IBus bus, IOperationTaskDispatcher taskDispatcher) : base(bus)
        {
            _taskDispatcher = taskDispatcher;
        }

        protected override void CorrelateMessages(ICorrelationConfig<CreateMachineSagaData> config)
        {
            base.CorrelateMessages(config);

            config.Correlate<OperationTaskStatusEvent<PlaceVirtualMachineCommand>>(m => m.OperationId, d => d.OperationId);
            config.Correlate<OperationTaskStatusEvent<ConvergeVirtualMachineCommand>>(m => m.OperationId, d => d.OperationId);
            config.Correlate<OperationTaskStatusEvent<PrepareVirtualMachineImageCommand>>(m => m.OperationId, d => d.OperationId);

        }

        public override Task Initiated(CreateMachineCommand message)
        {
            Data.Config = message.Config;

            var convergeMessage = new PlaceVirtualMachineCommand()
                { Config = message.Config, OperationId = message.OperationId, TaskId = Guid.NewGuid() };

            return _taskDispatcher.Send(convergeMessage);
        }

        public Task Handle(OperationTaskStatusEvent<PlaceVirtualMachineCommand> message)
        {
            return FailOrRun<PlaceVirtualMachineCommand,PlaceVirtualMachineResult>(message, (r) =>
            {
                Data.AgentName = r.AgentName;

                return _taskDispatcher.Send(new PrepareVirtualMachineImageCommand
                { ImageConfig = Data.Config.VM.Image, 
                  AgentName = r.AgentName, 
                  OperationId = message.OperationId, 
                  TaskId = Guid.NewGuid()
                });
            });

        }

        public Task Handle(OperationTaskStatusEvent<ConvergeVirtualMachineCommand> message)
        {
            return FailOrRun(message, () => Complete());

        }


        public Task Handle(OperationTaskStatusEvent<PrepareVirtualMachineImageCommand> message)
        {
            return FailOrRun(message, () =>
            {
                var convergeMessage = new ConvergeVirtualMachineCommand
                    { Config = Data.Config, AgentName = Data.AgentName, OperationId = message.OperationId, TaskId = Guid.NewGuid() };

                return _taskDispatcher.Send(convergeMessage);
            });
        }
    }
}