﻿using System.Collections.Generic;
using Eryph.ConfigModel;
using Eryph.Core;
using Eryph.Resources.Machines;

namespace Eryph.Messages.Resources.Catlets.Commands
{
    [SendMessageTo(MessageRecipient.Controllers)]
    public class UpdateInventoryCommand
    {
        [PrivateIdentifier]
        public string AgentName { get; set; }

        public List<VirtualMachineData> Inventory { get; set; }
    }
}