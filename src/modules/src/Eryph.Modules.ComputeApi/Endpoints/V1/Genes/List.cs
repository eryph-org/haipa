﻿using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eryph.Modules.AspNetCore.ApiProvider.Endpoints;
using Eryph.Modules.AspNetCore.ApiProvider.Handlers;
using Eryph.Modules.AspNetCore.ApiProvider.Model;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.ComputeApi.Model.V1;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Eryph.Modules.ComputeApi.Endpoints.V1.Genes;

public class List(
    IListRequestHandler<ListRequest, Gene, StateDb.Model.Gene> listRequestHandler,
    IListEntitySpecBuilder<ListRequest, StateDb.Model.Gene> specBuilder)
    : ListEntitiesEndpoint<ListRequest, Gene, StateDb.Model.Gene>(listRequestHandler, specBuilder)
{
    [Authorize(Policy = "compute:genes:read")]
    [HttpGet("genes")]
    [SwaggerOperation(
        Summary = "List all genes",
        Description = "List all genes",
        OperationId = "Genes_List",
        Tags = ["Genes"])
    ]
    [SwaggerResponse(Microsoft.AspNetCore.Http.StatusCodes.Status200OK, "Success", typeof(ListResponse<Gene>))]
    public override Task<ActionResult<ListResponse<Gene>>> HandleAsync(
        [FromRoute] ListRequest request,
        CancellationToken cancellationToken = default)
    {
        return base.HandleAsync(request, cancellationToken);
    }
}
