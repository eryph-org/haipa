﻿using System;
using Eryph.Resources.Machines;

namespace Eryph.Messages.Resources.Machines.Commands;

[SendMessageTo(MessageRecipient.VMHostAgent)]
public class UpdateVirtualCatletConfigDriveCommand : IVMCommand
{
    public Guid MachineId { get; set; }
    public Guid VMId { get; set; }

    public VirtualMachineMetadata MachineMetadata { get; set; }
    public MachineNetworkSettings[] MachineNetworkSettings { get; set; }

}