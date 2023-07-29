﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dbosoft.Rebus.Operations.Events;
using Dbosoft.Rebus.Operations.Workflow;
using Eryph.Messages.Resources.Networks.Commands;
using JetBrains.Annotations;
using Rebus.Handlers;
using Rebus.Sagas;

namespace Eryph.Modules.Controller.Networks
{
    [UsedImplicitly]
    internal class UpdateNetworksSaga : OperationTaskWorkflowSaga<UpdateNetworksCommand, UpdateNetworksSagaData>,
        IHandleMessages<OperationTaskStatusEvent<UpdateProjectNetworkPlanCommand>>

    {

        public UpdateNetworksSaga(IWorkflow workflow) : base(workflow)
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