﻿using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Ardalis.Specification;
using Eryph.Core;
using Eryph.StateDb;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;
using Microsoft.AspNetCore.Http;

namespace Eryph.Modules.AspNetCore;

public class UserRightsProvider : IUserRightsProvider
{
    private readonly IHttpContextAccessor _contextAccessor;
    private readonly IStateStore _stateStore;
    public UserRightsProvider(IHttpContextAccessor contextAccessor, IStateStore stateStore)
    {
        _contextAccessor = contextAccessor;
        _stateStore = stateStore;
    }

    public Guid GetUserTenantId()
    {
        var tenantId = Guid.Empty;
        var claims = _contextAccessor.HttpContext?.User.Claims.ToArray() ?? Array.Empty<Claim>();
        var tenantClaim = claims.FirstOrDefault(x => x.Type == "client_tenant");
        if (tenantClaim != null) _ = Guid.TryParse(tenantClaim.Value, out tenantId);

        return tenantId;
    }

    public Guid[] GetUserRoles()
    {
        var roles = Array.Empty<Guid>();

        var claims = _contextAccessor.HttpContext?.User.Claims.ToArray() ?? Array.Empty<Claim>();

        var rolesClaim = claims.FirstOrDefault(x => x.Type == "client_roles");
        if (rolesClaim != null)
        {
            roles = rolesClaim.Value.Split(',')
                .Select(x => (Guid.TryParse(x, out var guid), guid)).Where(x => x.Item1)
                .Select(x => x.guid).ToArray();
        }
        return roles;
    }


    public async Task<bool> HasResourceAccess(Guid resourceId, AccessRight requiredAccess)
    {
        var resource = await _stateStore.For<Resource>().GetBySpecAsync(new ResourceSpecs<Resource>.GetById(resourceId,
            q => q.Include(x => x.Project).ThenInclude(x => x.Roles)));
        
        if(resource == null) return false;

        return await HasResourceAccess(resource, requiredAccess);
    }

    public async Task<bool> HasResourceAccess(Resource resource, AccessRight requiredAccess)
    {
        if (resource.Project == null)
            await _stateStore.LoadPropertyAsync(resource, x => x.Project);

        if (resource.Project == null)
            return false;

        var projectRight = requiredAccess switch
        {
            AccessRight.None => AccessRight.None,
            AccessRight.Read => AccessRight.Read,
            AccessRight.Write => AccessRight.Write,
            AccessRight.Admin => AccessRight.Write,
            _ => throw new ArgumentOutOfRangeException(nameof(requiredAccess), requiredAccess, null)
        };

        return await HasProjectAccess(resource.Project, projectRight);

    }

    public async Task<bool> HasProjectAccess(Guid projectId, AccessRight requiredAccess)
    {
        var project = await _stateStore.For<Project>().GetByIdAsync(projectId);
        
        if(project == null) return false;
        
        return await HasProjectAccess(project, requiredAccess);
    }

    public async Task<bool> HasProjectAccess(Project project, AccessRight requiredAccess)
    {
        if (GetUserTenantId() != project.TenantId)
            return false;

        if (project.Roles == null)
            await _stateStore.LoadCollectionAsync(project, x => x.Roles);

        var userRoles = GetUserRoles();

        if (userRoles.Contains(EryphConstants.SuperAdminRole))
            return true;

        return userRoles.Select(userRole => project.Roles?.FirstOrDefault(x => x.RoleId == userRole))
            .Any(projectRole => projectRole != null && projectRole.AccessRight >= requiredAccess);
    }
}