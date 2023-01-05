﻿using System;
using System.Threading.Tasks;
using Eryph.Configuration;
using Eryph.Modules.Controller.DataServices;
using Eryph.Resources.Machines;
using LanguageExt;

namespace Eryph.Runtime.Zero.Configuration.VMMetadata
{
    internal class MetadataServiceWithConfigServiceDecorator : IVirtualMachineMetadataService
    {
        private readonly IConfigWriterService<VirtualCatletMetadata> _configService;
        private readonly IVirtualMachineMetadataService _decoratedService;

        public MetadataServiceWithConfigServiceDecorator(IVirtualMachineMetadataService decoratedService,
            IConfigWriterService<VirtualCatletMetadata> configService)
        {
            _decoratedService = decoratedService;
            _configService = configService;
        }


        public Task<Option<VirtualCatletMetadata>> GetMetadata(Guid id)
        {
            return _decoratedService.GetMetadata(id);
        }

        public async Task<Unit> SaveMetadata(VirtualCatletMetadata metadata)
        {
            await _decoratedService.SaveMetadata(metadata);
            await _configService.Update(metadata, "");
            return Unit.Default;
        }

        public async Task<Unit> RemoveMetadata(Guid id)
        {
            await _decoratedService.RemoveMetadata(id);
            await _configService.Delete(new VirtualCatletMetadata { Id = id }, "");
            return Unit.Default;
        }
    }
}