﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Eryph.StateDb.Model;
using Resource = Eryph.Resources.Resource;

namespace Eryph.ModuleCore
{
    public interface IOperationDispatcher
    {
        Task<Operation?> StartNew<T>(Guid tenantId, Resource resource = default) where T : class, new();

        Task<IEnumerable<Operation>> StartNew<T>(Guid tenantId, params Resource[] resources) where T : class, new();

        Task<Operation?> StartNew(Guid tenantId, Type commandType, Resource resource = default);
        Task<IEnumerable<Operation>> StartNew(Guid tenantId, Type commandType, params Resource[] resources);
        Task<Operation?> StartNew(Guid tenantId, object operationCommand);

        Task<IEnumerable<Operation>> StartNew(Guid tenantId, object command, params Resource[] resources);
    }
}