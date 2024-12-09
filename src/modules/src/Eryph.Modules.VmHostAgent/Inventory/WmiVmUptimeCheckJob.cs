﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.Rebus.Operations;
using Eryph.Messages.Resources.Catlets.Events;
using Eryph.VmManagement.Inventory;
using Eryph.VmManagement.Sys;
using Eryph.VmManagement.Wmi;
using LanguageExt;
using LanguageExt.Common;
using Microsoft.Extensions.Logging;
using Quartz;
using Rebus.Bus;
using SimpleInjector;

using static LanguageExt.Prelude;

namespace Eryph.Modules.VmHostAgent.Inventory;

internal class WmiVmUptimeCheckJob(Container container) : IJob
{
    private readonly IBus _bus = container.GetInstance<IBus>();
    private readonly ILoggerFactory _loggerFactory = container.GetInstance<ILoggerFactory>();
    private readonly WorkflowOptions _workflowOptions = container.GetInstance<WorkflowOptions>();

    public async Task Execute(IJobExecutionContext context)
    {
        var result = WmiVmUptimeCheckJob<SimpleAgentRuntime>
            .Execute()
            .Run(SimpleAgentRuntime.New(_loggerFactory));

        var messages = result.ThrowIfFail();
        foreach (var message in messages)
        {
            await _bus.SendWorkflowEvent(_workflowOptions, message);
        }
    }
}

internal static class WmiVmUptimeCheckJob<RT> where RT : struct, HasWmi<RT>
{
    public static Eff<RT, Seq<CatletStateChangedEvent>> Execute() =>
        from _ in SuccessEff<RT, Unit>(unit)
        let timestamp = DateTimeOffset.UtcNow
        // The uptime check only considers machines started within the last hour.
        // For longer running machines the inventory job takes care of updating uptime. 
        // The uptime only needs to be accurate during the early start phase to check if
        // the deployment has succeeded and to handle the removal of sensitive data from
        // the cloud-init configs.
        from changedVms in Wmi<RT>.executeQuery(
            @"root\virtualization\v2",
            Seq("__CLASS", "Name", "EnabledState", "OtherEnabledState", "HealthState", "OnTimeInMilliseconds"),
            "Msvm_ComputerSystem",
            Some("OnTimeInMilliseconds <> NULL AND OnTimeInMilliseconds < 3600000"))
        from messages in changedVms
            .Map(vm => createMessage(vm, timestamp))
            .Sequence()
        select messages;

    private static Eff<CatletStateChangedEvent> createMessage(
        WmiObject vm,
        DateTimeOffset timestamp) =>
        from vmId in WmiMsvmUtils.GetVmId(vm)
        from validVmId in vmId
            .ToEff(Error.New("The WMI object does contain a VM ID."))
        from vmState in WmiMsvmUtils.getVmState(vm)
        from upTime in WmiMsvmUtils.getVmUpTime(vm)
        let message = new CatletStateChangedEvent
        {
            VmId = validVmId,
            Status = VmStateUtils.toVmStatus(vmState),
            UpTime = upTime,
            Timestamp = timestamp,
        }
        select message;
}
