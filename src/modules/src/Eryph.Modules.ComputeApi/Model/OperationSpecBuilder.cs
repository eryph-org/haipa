﻿using System;
using Ardalis.Specification;
using Eryph.Modules.AspNetCore;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.ComputeApi.Endpoints.V1.Operations;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;

namespace Eryph.Modules.ComputeApi.Model
{
    public class OperationSpecBuilder : ISingleEntitySpecBuilder<OperationRequest, OperationModel>, IListEntitySpecBuilder<OperationsListRequest, OperationModel>
    {
        readonly IUserRightsProvider _userRightsProvider;

        public OperationSpecBuilder(IUserRightsProvider userRightsProvider)
        {
            _userRightsProvider = userRightsProvider;
        }

        public ISingleResultSpecification<OperationModel> GetSingleEntitySpec(OperationRequest request, AccessRight accessRight)
        {
            if (!Guid.TryParse(request.Id, out var operationId))
                throw new ArgumentException("The ID is not a GUID.", nameof(request));
            
            return new OperationSpecs.GetById(
                operationId,
                _userRightsProvider.GetAuthContext(),
                _userRightsProvider.GetProjectRoles(AccessRight.Read),
                request.Expand,
                request.LogTimestamp);
        }

        public ISpecification<OperationModel> GetEntitiesSpec(OperationsListRequest request)
        {
            return new OperationSpecs.GetAll(
                _userRightsProvider.GetAuthContext(),
                _userRightsProvider.GetProjectRoles(AccessRight.Read),
                request.Expand,
                request.LogTimestamp);
        }
    }
}