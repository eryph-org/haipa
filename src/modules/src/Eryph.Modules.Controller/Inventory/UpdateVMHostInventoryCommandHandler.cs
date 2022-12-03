﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Eryph.Messages.Resources.Machines.Commands;
using Eryph.ModuleCore;
using Eryph.Modules.Controller.DataServices;
using Eryph.StateDb;
using Eryph.StateDb.Model;
using JetBrains.Annotations;
using Rebus.Handlers;

namespace Eryph.Modules.Controller.Inventory
{
    [UsedImplicitly]
    internal class UpdateVMHostInventoryCommandHandler : UpdateInventoryCommandHandlerBase,
        IHandleMessages<UpdateVMHostInventoryCommand>
    {
        private readonly IVMHostMachineDataService _vmHostDataService;

        public UpdateVMHostInventoryCommandHandler(
            IVirtualMachineMetadataService metadataService, 
            IOperationDispatcher dispatcher,
            IVirtualMachineDataService vmDataService,
            IVirtualDiskDataService vhdDataService, 
            IVMHostMachineDataService vmHostDataService, IStateStore stateStore) : 
            base(metadataService, dispatcher, vmDataService, vhdDataService, stateStore)
        {
            _vmHostDataService = vmHostDataService;
        }

        public async Task Handle(UpdateVMHostInventoryCommand message)
        {
            var newMachineState = await
                _vmHostDataService.GetVMHostByHardwareId(message.HostInventory.HardwareId).IfNoneAsync(
                async () => new VirtualCatletHost
                {
                    Id = Guid.NewGuid(),
                    AgentName = message.HostInventory.Name,
                    Name = message.HostInventory.Name,
                    HardwareId = message.HostInventory.HardwareId,
                    Project = await FindRequiredProject("default")
                });

            newMachineState.Status = CatletStatus.Running;

            var existingMachine = await _vmHostDataService.GetVMHostByHardwareId(message.HostInventory.HardwareId)
                .IfNoneAsync(() => _vmHostDataService.AddNewVMHost(newMachineState));

            await UpdateVMs(message.VMInventory, existingMachine);

        }
    }
}