﻿using Ardalis.Specification;
using Eryph.Modules.AspNetCore.ApiProvider.Model;

namespace Eryph.Modules.AspNetCore.ApiProvider
{
    public interface ISingleEntitySpecBuilder<in TRequest,T> where T : class
        where TRequest : SingleEntityRequest
    {

        ISingleResultSpecification<T> GetSingleEntitySpec(TRequest request);
    }
}