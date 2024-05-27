﻿using System;
using System.Collections.Generic;

namespace Eryph.StateDb.Model;

// The change tracking in the controller module must be updated when modifying this entity.
public class Project
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public Guid TenantId { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;

    public virtual List<Resource> Resources { get; set; } = null!;

    public virtual List<ProjectRoleAssignment> ProjectRoles { get; set; } = null!;

    public virtual List<OperationTaskModel> ReferencedTasks { get; set; } = null!;
}
