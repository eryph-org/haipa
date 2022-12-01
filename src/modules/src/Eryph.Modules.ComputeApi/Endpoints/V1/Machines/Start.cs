﻿using System.Threading;
using System.Threading.Tasks;
using Eryph.Messages.Resources.Machines.Commands;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.AspNetCore.ApiProvider.Endpoints;
using Eryph.Modules.AspNetCore.ApiProvider.Handlers;
using Eryph.Modules.AspNetCore.ApiProvider.Model;
using Eryph.Modules.AspNetCore.ApiProvider.Model.V1;
using Eryph.Resources;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

using static Microsoft.AspNetCore.Http.StatusCodes;

namespace Eryph.Modules.ComputeApi.Endpoints.V1.Machines
{
    public class Start : ResourceOperationEndpoint<SingleResourceRequest, StateDb.Model.Catlet>
    {

        public Start([NotNull] IResourceOperationHandler<StateDb.Model.Catlet> operationHandler) : base(operationHandler)
        {
        }

        protected override object CreateOperationMessage(StateDb.Model.Catlet model, SingleResourceRequest request)
        {
            return new StartMachineCommand{Resource = new Resource(ResourceType.Machine, model.Id)};
        }


        [HttpPut("machines/{id}/start")]
        [SwaggerOperation(
            Summary = "Starts a Machine",
            Description = "Starts a Machine",
            OperationId = "Machines_Start",
            Tags = new[] { "Machines" })
        ]

        public override Task<ActionResult<ListResponse<Operation>>> HandleAsync([FromRoute] SingleResourceRequest request, CancellationToken cancellationToken = default)
        {
            return base.HandleAsync(request, cancellationToken);
        }


    }
}
