﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Eryph.Messages.Projects;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.AspNetCore.ApiProvider.Endpoints;
using Eryph.Modules.AspNetCore.ApiProvider.Handlers;
using Eryph.Modules.AspNetCore.ApiProvider.Model;
using Eryph.StateDb.Model;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Operation = Eryph.Modules.AspNetCore.ApiProvider.Model.V1.Operation;


namespace Eryph.Modules.CommonApi.Endpoints.V1.Projects
{
    public class Delete : OperationRequestEndpoint<SingleEntityRequest, Project>
    {
        public Delete([NotNull] IOperationRequestHandler<Project> operationHandler, 
            [NotNull] ISingleEntitySpecBuilder<SingleEntityRequest, Project> specBuilder) : base(operationHandler, specBuilder)
        {
        }

        [HttpDelete("projects/{id}")]
        [SwaggerOperation(
            Summary = "Deletes a project",
            Description = "Deletes a project",
            OperationId = "Projects_Delete",
            Tags = new[] { "Projects" })
        ]
        public override Task<ActionResult<ListResponse<Operation>>> HandleAsync([FromRoute] SingleEntityRequest request, CancellationToken cancellationToken = default)
        {
            return base.HandleAsync(request, cancellationToken);
        }


        protected override object CreateOperationMessage(Project model, SingleEntityRequest request)
        {
            return new DestroyProjectCommand { CorrelationId = Guid.NewGuid(), ProjectId = Guid.Parse(request.Id) };
        }
    }
}
