﻿using Haipa.Messages.Operations.Commands;
using Haipa.Primitives;
using Haipa.Primitives.Resources.Machines.Config;

namespace Haipa.Messages.Resources.Machines.Commands
{
    [SendMessageTo(MessageRecipient.VMHostAgent)]
    public class CreateVirtualMachineCommand : OperationTaskCommand, IHostAgentCommand
    {
        public MachineConfig Config { get; set; }
        public string AgentName { get; set; }
        public long NewMachineId { get; set; }

    }
}