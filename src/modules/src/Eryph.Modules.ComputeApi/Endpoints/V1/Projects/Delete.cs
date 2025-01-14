﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Eryph.Messages.Projects;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.AspNetCore.ApiProvider.Endpoints;
using Eryph.Modules.AspNetCore.ApiProvider.Handlers;
using Eryph.Modules.AspNetCore.ApiProvider.Model;
using Eryph.StateDb.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Operation = Eryph.Modules.AspNetCore.ApiProvider.Model.V1.Operation;

namespace Eryph.Modules.ComputeApi.Endpoints.V1.Projects;

public class Delete(
    IEntityOperationRequestHandler<Project> operationHandler,
    ISingleEntitySpecBuilder<SingleEntityRequest, Project> specBuilder)
    : OperationRequestEndpoint<SingleEntityRequest, Project>(operationHandler, specBuilder)
{
    [Authorize(Policy = "compute:projects:write")]
    [HttpDelete("projects/{id}")]
    [SwaggerOperation(
        Summary = "Delete a project",
        Description = "Delete a project",
        OperationId = "Projects_Delete",
        Tags = ["Projects"])
    ]
    public override async Task<ActionResult<Operation>> HandleAsync(
        [FromRoute] SingleEntityRequest request,
        CancellationToken cancellationToken = default)
    {
        return await base.HandleAsync(request, cancellationToken);
    }

    protected override object CreateOperationMessage(Project model, SingleEntityRequest request)
    {
        return new DestroyProjectCommand
        {
            CorrelationId = Guid.NewGuid(),
            ProjectId = Guid.Parse(request.Id)
        };
    }
}
