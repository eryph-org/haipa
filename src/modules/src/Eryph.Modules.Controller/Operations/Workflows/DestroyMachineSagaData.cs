﻿using System;
using System.Collections.Generic;
using LanguageExt;

namespace Eryph.Modules.Controller.Operations.Workflows
{
    public class DestroyMachineSagaData : TaskWorkflowSagaData
    {
        public Guid MachineId { get; set; }
    }
}