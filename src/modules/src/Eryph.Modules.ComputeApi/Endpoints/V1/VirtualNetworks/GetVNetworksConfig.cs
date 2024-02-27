﻿using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using Eryph.Core;
using Eryph.Modules.AspNetCore;
using Eryph.Modules.AspNetCore.ApiProvider.Endpoints;
using Eryph.Modules.AspNetCore.ApiProvider.Handlers;
using Eryph.Modules.AspNetCore.ApiProvider.Model;
using Eryph.Modules.ComputeApi.Model;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace Eryph.Modules.ComputeApi.Endpoints.V1.VirtualNetworks
{
    public class GetVNetworksConfig : SingleResultEndpoint<ProjectRequest, VirtualNetworkConfiguration, Project>
    {
        private readonly IUserRightsProvider _userRightsProvider;

        public GetVNetworksConfig([NotNull] IGetRequestHandler<Project, VirtualNetworkConfiguration> requestHandler, IUserRightsProvider userRightsProvider) : base(requestHandler)
        {
            _userRightsProvider = userRightsProvider;
        }

        protected override ISingleResultSpecification<Project> CreateSpecification(ProjectRequest request)
        {
            var sufficientRoles = _userRightsProvider.GetProjectRoles(AccessRight.Read);

            return new ProjectSpecs.GetById(request.ProjectId,
                _userRightsProvider.GetAuthContext(), sufficientRoles);
        }
        [Authorize(Policy = "compute:projects:read")]
        // ReSharper disable once StringLiteralTypo
        [HttpGet("projects/{projectId}/vnetworks/config")]
        [SwaggerOperation(
            Summary = "Get project virtual networks configuration",
            Description = "Get the configuration for all networks in a project",
            OperationId = "VNetworks_GetConfig",
            Tags = new[] { "Virtual Networks" })
        ]
        [SwaggerResponse(Microsoft.AspNetCore.Http.StatusCodes.Status200OK, "Success", typeof(VirtualNetworkConfiguration))]

        public override Task<ActionResult<VirtualNetworkConfiguration>> HandleAsync([FromRoute] ProjectRequest request, CancellationToken cancellationToken = default)
        {
            return base.HandleAsync(request, cancellationToken);
        }



    }
}
