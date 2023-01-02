﻿using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eryph.Core;
using Eryph.Core.Network;
using Eryph.ModuleCore;
using LanguageExt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Eryph.Modules.VmHostAgent;

internal class SyncServiceCommand
{
    public string CommandName { get; set; }
    public JsonElement? Data { get; set; }
}

internal class SyncServiceResponse
{
    public string Response { get; set; }
    public JsonElement? Data { get; set; }
}

internal class SyncService : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IAgentControlService _controlService;
    private readonly INetworkSyncService _networkSyncService;
    public SyncService(ILogger<SyncService> logger, 
        IAgentControlService controlService, INetworkSyncService networkSyncService)
    {
        _logger = logger;
        _controlService = controlService;
        _networkSyncService = networkSyncService;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        while (!stoppingToken.IsCancellationRequested)
        {
            await using var pipeServer =
                NamedPipeServerStreamAcl.Create("eryph_hostagent_sync",

                    PipeDirection.InOut,
                    1,
                    PipeTransmissionMode.Message,
                    PipeOptions.Asynchronous,
                    0, 0, CreateSystemIOPipeSecurity());

            var isIdle = true;
            try
            {
                await pipeServer.WaitForConnectionAsync(stoppingToken);
                isIdle = false;
                var ss = new StreamString(pipeServer);


                var commandString= await ss.ReadString(stoppingToken);
                var command = JsonSerializer.Deserialize<SyncServiceCommand>(commandString);

                var hasPermission = false;
                var commandValid = true;
                switch (command?.CommandName)
                {
                    case "STATUS":
                    {
                        hasPermission = true;
                        break;
                    }
                    case "VALIDATE_CHANGES": break;
                    case "REBUILD_NETWORKS": break;
                    case "STOP_OVN": break;
                    case "START_OVN": break;
                    default:
                        commandValid = false;
                        break;
                }

                if (commandValid && !hasPermission)
                {
                    pipeServer.RunAsClient(() =>
                    {
                        AdminGuard.InElevatedProcess(() => Unit.Default,
                            () =>
                            {
                                hasPermission = true;
                                return Unit.Default;
                            });
                    });
                }

                if (!commandValid)
                    await ss.WriteResponse(new SyncServiceResponse{Response = "INVALID"}, stoppingToken);
                else
                {
                    if (hasPermission)
                    {
                        var response = await RunCommand(command);
                        await ss.WriteResponse(response, stoppingToken);
                    }
                    else
                        await ss.WriteResponse(new SyncServiceResponse { Response = "PERMISSION_DENIED" }, stoppingToken);
                }

                pipeServer.WaitForPipeDrain();
            }
            catch (Exception ex)
            {
                if (!isIdle)
                    _logger.LogDebug(ex, "Exception in sync service");
            }
            finally
            {
                try
                {
                    pipeServer.Disconnect();
                }
                catch (Exception)
                {

                }
            }
        }
    }

    private async Task<SyncServiceResponse> RunCommand(SyncServiceCommand command)
    {
        AgentService service;
        AgentServiceOperation operation;
        switch (command.CommandName)
        {
            case "STATUS": return new SyncServiceResponse
            {
                Response = "DONE", Data = JsonSerializer.SerializeToElement(true)
            };
            case "VALIDATE_CHANGES":
                var networkProviders = command.Data.HasValue 
                    ? command.Data.Value.Deserialize<NetworkProvider[]>()
                    : Array.Empty<NetworkProvider>();
                return await _networkSyncService.ValidateChanges(networkProviders)
                    .Match(r =>
                        {
                            return new SyncServiceResponse
                            {
                                Response = "DONE",
                                Data = JsonSerializer.SerializeToElement(r)
                            };
                        },
                        _ => new SyncServiceResponse{ Response = "FAILED"});
            case "REBUILD_NETWORKS":
                return new SyncServiceResponse {
                Response = await _networkSyncService.SyncNetworks(CancellationToken.None)
                        .Match(r => "DONE",
                            _ => "FAILED")};
            case "STOP_OVN":
                service = AgentService.OVNController;
                operation = AgentServiceOperation.Stop;
                break;
            case "START_OVN":
                service = AgentService.OVNController;
                operation = AgentServiceOperation.Start;
                break;
            default: return new SyncServiceResponse { Response = "INVALID" };
        }

        var succeeded = await _controlService.SendControlEvent(
            service, operation, CancellationToken.None);

        return new SyncServiceResponse { Response = succeeded ? "DONE" : "FAILED" };

    }

    private static PipeSecurity CreateSystemIOPipeSecurity()
    {
        var pipeSecurity = new PipeSecurity();

        var id = new SecurityIdentifier(
            WellKnownSidType.AuthenticatedUserSid, null);

        // Allow Everyone read and write access to the pipe. 
        pipeSecurity.SetAccessRule(
            new PipeAccessRule(id, PipeAccessRights.ReadWrite, 
                AccessControlType.Allow));

        return pipeSecurity;
    }


}