﻿using System;
using Ardalis.Specification;
using Eryph.Modules.AspNetCore;
using Eryph.Modules.AspNetCore.ApiProvider;
using Eryph.Modules.ComputeApi.Endpoints.V1.ProjectMembers;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;

namespace Eryph.Modules.ComputeApi.Model;

public class ProjectMemberSpecBuilder :
    ISingleEntitySpecBuilder<ProjectMemberRequest, ProjectRoleAssignment>,
    IListEntitySpecBuilder<ProjectMembersListRequest, ProjectRoleAssignment>
{
    private readonly IUserRightsProvider _userRightsProvider;

    public ProjectMemberSpecBuilder(IUserRightsProvider userRightsProvider)
    {
        _userRightsProvider = userRightsProvider;
    }

    public ISingleResultSpecification<ProjectRoleAssignment> GetSingleEntitySpec(ProjectMemberRequest request, AccessRight accessRight)
    {
        if (!Guid.TryParse(request.Id, out var memberId))
            throw new ArgumentException("The ID is not a GUID", nameof(request));

        return new ProjectRoleAssignmentSpecs.GetById(
            memberId,
            request.Project,
            _userRightsProvider.GetAuthContext(),
            _userRightsProvider.GetProjectRoles(accessRight));
    }

    public ISpecification<ProjectRoleAssignment> GetEntitiesSpec(ProjectMembersListRequest request)
    {
        return new ProjectRoleAssignmentSpecs.GetByProject(
            request.ProjectId.GetValueOrDefault(),
            _userRightsProvider.GetAuthContext(),
            _userRightsProvider.GetProjectRoles(AccessRight.Read));
    }
}
