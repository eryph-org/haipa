﻿using System.Threading;
using System.Threading.Tasks;
using Eryph.Messages.Resources.Catlets.Commands;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.AspNetCore.ApiProvider.Endpoints;
using Eryph.Modules.AspNetCore.ApiProvider.Handlers;
using Eryph.Modules.AspNetCore.ApiProvider.Model;
using Eryph.Resources;
using Eryph.StateDb.Model;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Operation = Eryph.Modules.AspNetCore.ApiProvider.Model.V1.Operation;
using Resource = Eryph.Resources.Resource;

namespace Eryph.Modules.ComputeApi.Endpoints.V1.Catlets
{
    public class Delete : ResourceOperationEndpoint<SingleEntityRequest, Catlet>
    {


        public Delete([NotNull] IOperationRequestHandler<Catlet> operationHandler, 
            [NotNull] ISingleEntitySpecBuilder<SingleEntityRequest, Catlet> specBuilder) : base(operationHandler, specBuilder)
        {
        }

        protected override object CreateOperationMessage(Catlet model, SingleEntityRequest request)
        {
            return new DestroyCatletCommand{ CatletId = model.Id};
        }

        [Authorize(Policy = "compute:catlets:write")]
        [HttpDelete("catlets/{id}")]
        [SwaggerOperation(
            Summary = "Deletes a catlet",
            Description = "Deletes a catlet",
            OperationId = "Catlets_Delete",
            Tags = new[] { "Catlets" })
        ]

        public override Task<ActionResult<ListResponse<Operation>>> HandleAsync([FromRoute] SingleEntityRequest request, CancellationToken cancellationToken = default)
        {
            return base.HandleAsync(request, cancellationToken);
        }


    }
}
