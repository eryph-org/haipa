﻿using Haipa.Primitives;
using Haipa.Primitives.Resources.Machines;

namespace Haipa.Messages.Resources.Machines.Commands
{
    public class ConvergeVirtualMachineResult
    {
        public VirtualMachineMetadata MachineMetadata { get; set; }
        public VirtualMachineData Inventory { get; set; }
    }
}