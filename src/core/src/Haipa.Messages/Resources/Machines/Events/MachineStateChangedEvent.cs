﻿using System;
using Haipa.Resources.Machines;

namespace Haipa.Messages.Resources.Machines.Events
{
    [SubscribesMessage(MessageSubscriber.Controllers)]
    public class MachineStateChangedEvent
    {
        public Guid MachineId { get; set; }
        public VmStatus Status { get; set; }
    }
}