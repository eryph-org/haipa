﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.Rebus.Operations;
using Eryph.Messages.Resources.Catlets.Commands;
using Eryph.Modules.VmHostAgent.Networks.OVS;
using Eryph.VmManagement;
using Eryph.VmManagement.Data;
using Eryph.VmManagement.Data.Full;
using LanguageExt;
using Rebus.Handlers;

using static LanguageExt.Prelude;

namespace Eryph.Modules.VmHostAgent;

internal class SyncVmNetworkPortsCommandHandler(
    IOVSPortManager ovsPortManager,
    IPowershellEngine powershellEngine,
    ITaskMessaging messaging)
    : IHandleMessages<OperationTask<SyncVmNetworkPortsCommand>>
{
    public Task Handle(OperationTask<SyncVmNetworkPortsCommand> message) =>
        SyncPorts(message.Command).FailOrComplete(messaging, message);

    private Aff<Unit> SyncPorts(SyncVmNetworkPortsCommand command) =>
        from vmInfo in GetVm(command.VMId)
        from _ in vmInfo
            // This command is only used to sync the ports when the network adapters
            // of a running VM have been modified. A different event and handler
            // sync the ports when a VM is started or stopped.
            .Filter(v => v.Value.State is VirtualMachineState.Running or VirtualMachineState.RunningCritical)
            .Map(v => ovsPortManager.SyncPorts(v, VMPortChange.Add).ToAff(identity))
            .SequenceSerial()
        select unit;

    private Aff<Option<TypedPsObject<VirtualMachineInfo>>> GetVm(
        Guid vmId) =>
        from _ in SuccessAff(unit)
        let command = PsCommandBuilder.Create()
            .AddCommand("Get-VM")
            .AddParameter("Id", vmId)
            .AddParameter("ErrorAction", "SilentlyContinue")
        from vmInfos in powershellEngine.GetObjectsAsync<VirtualMachineInfo>(command)
            .ToAff()
        select vmInfos.HeadOrNone();
}
