﻿using System;
using System.Threading.Tasks;
using Eryph.Messages.Resources.Commands;
using Eryph.Messages.Resources.Events;
using JetBrains.Annotations;
using Rebus.Bus;
using Rebus.Handlers;

namespace Eryph.Modules.VmHostAgent
{
    [UsedImplicitly]
    public class VerifyPlacementCalculationCommandHandler : IHandleMessages<VerifyPlacementCalculationCommand>
    {
        private readonly IBus _bus;

        public VerifyPlacementCalculationCommandHandler(IBus bus)
        {
            _bus = bus;
        }

        public Task Handle(VerifyPlacementCalculationCommand message)
        {
            //this is a placeholder for a real verification that should make sure 
            //placement data used for calculation can be confirmed by agent

            return _bus.Publish(new PlacementVerificationCompletedEvent
            {
                AgentName = Environment.MachineName,
                Confirmed = true,
                CorrelationId = message.CorrelationId
            });
        }
    }
}