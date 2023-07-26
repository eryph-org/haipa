﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eryph.Messages.Operations.Events;
using Eryph.Messages.Resources.Networks.Commands;
using Eryph.ModuleCore;
using Eryph.Modules.Controller.Operations;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Sagas;

namespace Eryph.Modules.Controller.Networks
{
    [UsedImplicitly]
    internal class UpdateNetworksSaga : OperationTaskWorkflowSaga<UpdateNetworksCommand, UpdateNetworksSagaData>,
        IHandleMessages<OperationTaskStatusEvent<UpdateProjectNetworkPlanCommand>>

    {

        public UpdateNetworksSaga(IBus bus, IOperationTaskDispatcher taskDispatcher, IMessageContext messageContext) : base(bus, taskDispatcher, messageContext)
        {
        }

        protected override void CorrelateMessages(ICorrelationConfig<UpdateNetworksSagaData> config)
        {
            base.CorrelateMessages(config);
            config.Correlate<OperationTaskStatusEvent<UpdateProjectNetworkPlanCommand>>(m => m.InitiatingTaskId,
                d => d.SagaTaskId);

        }

        protected override async Task Initiated(UpdateNetworksCommand message)
        {
            Data.ProjectIds = message.Projects;

            foreach (var project in message.Projects)
            {
                await StartNewTask(
                    new UpdateProjectNetworkPlanCommand
                    {
                        ProjectId = project
                    });
            }

        }

        public Task Handle(OperationTaskStatusEvent<UpdateProjectNetworkPlanCommand> message)
        {
            return FailOrRun<UpdateProjectNetworkPlanCommand, UpdateProjectNetworkPlanResponse>(message,
                async response =>
                {
                    Data.ProjectsCompleted ??= new List<Guid>();

                    // ignore if already received
                    if (Data.ProjectsCompleted.Contains(response.ProjectId))
                        return;

                    Data.ProjectsCompleted.Add(response.ProjectId);

                    if (Data.ProjectsCompleted.Count == Data.ProjectIds?.Length)
                        await Complete();
                });
        }
    }
}