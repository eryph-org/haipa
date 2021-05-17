﻿using Haipa.Messages.Events;
using Haipa.Messages.Operations;
using Haipa.VmConfig;

namespace Haipa.Messages.Commands.OperationTasks
{
    [SendMessageTo(MessageRecipient.VMHostAgent)]
    public class CreateVirtualMachineCommand : OperationTaskCommand, IHostAgentCommand
    {
        public MachineConfig Config { get; set; }
        public string AgentName { get; set; }
        public long NewMachineId { get; set; }

    }

    public class ConvergeVirtualMachineResult
    {
        public VirtualMachineMetadata MachineMetadata { get; set; }
        public VirtualMachineInfo Inventory { get; set; }
    }
}