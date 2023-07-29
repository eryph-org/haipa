﻿using System;
using Eryph.Resources;
using Eryph.StateDb.Workflows;

namespace Eryph.StateDb.Model
{
    public class OperationResourceModel
    {
        public Guid Id { get; set; }
        public Guid ResourceId { get; set; }
        public ResourceType ResourceType { get; set; }

        public virtual OperationModel Operation { get; set; }
    }

    public class OperationProjectModel
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public Project Project{ get; set; }

        public virtual OperationModel Operation { get; set; }
    }
}