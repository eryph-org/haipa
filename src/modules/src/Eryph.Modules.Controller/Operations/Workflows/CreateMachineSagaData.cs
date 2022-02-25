﻿using System;
using Eryph.Resources.Machines.Config;

namespace Eryph.Modules.Controller.Operations.Workflows
{
    public class CreateMachineSagaData : TaskWorkflowSagaData
    {
        public MachineConfig? Config { get; set; }
        public string? AgentName { get; set; }

        public CreateVMState State { get; set; }
        public Guid MachineId { get; set; }
    }
}