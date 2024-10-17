﻿using System;
using System.Reflection.PortableExecutable;
using Dbosoft.Rebus.Operations;
using Eryph.ConfigModel.Catlets;
using Eryph.Core;
using Eryph.Core.VmAgent;
using Eryph.Messages.Resources.Catlets.Commands;
using Eryph.Modules.VmHostAgent.Inventory;
using Eryph.Resources.Machines;
using Eryph.VmManagement;
using Eryph.VmManagement.Data.Core;
using Eryph.VmManagement.Data.Full;
using Eryph.VmManagement.Storage;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;

using static LanguageExt.Prelude;

// ReSharper disable ArgumentsStyleAnonymousFunction

namespace Eryph.Modules.VmHostAgent
{
    [UsedImplicitly]
    internal class CreateCatletVMCommandHandler : 
        CatletConfigCommandHandler<CreateCatletVMCommand, ConvergeCatletResult>
    {
        private readonly IHostArchitectureProvider _hostArchitectureProvider;
        private readonly IHostInfoProvider _hostInfoProvider;
        private readonly IHostSettingsProvider _hostSettingsProvider;
        private readonly IVmHostAgentConfigurationManager _vmHostAgentConfigurationManager;

        public CreateCatletVMCommandHandler(
            IPowershellEngine engine,
            ITaskMessaging messaging,
            ILogger log,
            IHostArchitectureProvider hostArchitectureProvider,
            IHostInfoProvider hostInfoProvider,
            IHostSettingsProvider hostSettingsProvider,
            IVmHostAgentConfigurationManager vmHostAgentConfigurationManager)
            : base(engine, messaging, log)
        {
            _hostArchitectureProvider = hostArchitectureProvider;
            _hostInfoProvider = hostInfoProvider;
            _hostSettingsProvider = hostSettingsProvider;
            _vmHostAgentConfigurationManager = vmHostAgentConfigurationManager;
        }

        protected override EitherAsync<Error, ConvergeCatletResult> HandleCommand(
            CreateCatletVMCommand command) =>
            from hostSettings in _hostSettingsProvider.GetHostSettings()
            from vmHostAgentConfig in _vmHostAgentConfigurationManager.GetCurrentConfiguration(hostSettings)
            from plannedStorageSettings in VMStorageSettings.Plan(
                vmHostAgentConfig, LongToString(command.StorageId), command.Config, None)
            from createdVM in CreateVM(plannedStorageSettings, Engine, command.BredConfig)
            let metadataId = Guid.NewGuid()
            let metadata = new CatletMetadata
            {
                Id = Guid.NewGuid(),
                MachineId = command.NewMachineId,
                VMId = createdVM.Value.Id,
                Fodder = command.Config.Fodder,
                Variables = command.Config.Variables,
                Parent = command.Config.Parent,
                Architecture = _hostArchitectureProvider.Architecture.Value,
            }
            from _ in SetMetadataId(createdVM, metadataId)
            from inventory in CreateMachineInventory(Engine, vmHostAgentConfig, createdVM, _hostInfoProvider)
            select new ConvergeCatletResult
            {
                VmId = createdVM.Value.Id,
                MetadataId = metadataId,
                Inventory = inventory,
                Timestamp = DateTimeOffset.UtcNow,
                Architecture = _hostArchitectureProvider.Architecture,
            };

        private static EitherAsync<Error, TypedPsObject<VirtualMachineInfo>> CreateVM(
            VMStorageSettings storageSettings,
            IPowershellEngine engine,
            CatletConfig config) =>
            from storageIdentifier in storageSettings.StorageIdentifier
                .ToEitherAsync(Error.New(
                    "Cannot create catlet. The storage identifier is missing."))
            let startupMemory = config.Memory?.Startup ?? 1024
            from vm in VirtualMachine.Create(engine, config.Name, storageIdentifier,
                storageSettings.VMPath, startupMemory)
            select vm;
    }
}