﻿using System.Collections.Generic;
using Haipa.Primitives;
using Haipa.Primitives.Resources.Machines;

namespace Haipa.Messages.Resources.Machines.Commands
{
    [SendMessageTo(MessageRecipient.Controllers)]
    public class UpdateInventoryCommand
    {
        public string AgentName { get; set; }

        public List<VirtualMachineData> Inventory { get; set; }


    }
}