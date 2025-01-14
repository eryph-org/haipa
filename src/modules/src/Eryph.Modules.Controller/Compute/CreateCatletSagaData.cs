﻿using System;
using System.Collections.Generic;
using Dbosoft.Rebus.Operations.Workflow;
using Eryph.ConfigModel;
using Eryph.ConfigModel.Catlets;
using Eryph.Core.Genetics;
using Eryph.ModuleCore;

namespace Eryph.Modules.Controller.Compute
{
    public class CreateCatletSagaData : TaskWorkflowSagaData
    {
        public CatletConfig? Config { get; set; }

        public CatletConfig? ParentConfig { get; set; }

        public CatletConfig? BredConfig { get; set; }

        public string? AgentName { get; set; }

        public CreateVMState State { get; set; }

        public Guid MachineId { get; set; }
        
        public Guid TenantId { get; set; }

        public Architecture? Architecture { get; set; }

        public IReadOnlyList<UniqueGeneIdentifier> ResolvedGenes { get; set; }
    }
}