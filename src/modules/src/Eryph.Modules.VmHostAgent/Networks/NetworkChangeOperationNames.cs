﻿using System;

namespace Eryph.Modules.VmHostAgent.Networks;

public class NetworkChangeOperationNames
{
    public static NetworkChangeOperationNames Instance { get; } = new();

    public string this[NetworkChangeOperation key]{

        get
        {
            return key switch
            {
                NetworkChangeOperation.StartOVN => "Start overlay network controller",
                NetworkChangeOperation.StopOVN => "Stop overlay network controller",
                NetworkChangeOperation.CreateOverlaySwitch => "Create eryph overlay switch",
                NetworkChangeOperation.RebuildOverLaySwitch => "Rebuild eryph overlay switch",
                NetworkChangeOperation.RemoveOverlaySwitch => "Remove eryph overlay switch",
                NetworkChangeOperation.DisconnectVMAdapters => "Disconnect V-Catlets from overlay switch",
                NetworkChangeOperation.ConnectVMAdapters => "Reconnect V-Catlets to overlay switch",
                NetworkChangeOperation.RemoveBridge => "Remove bridge '{0}'",
                NetworkChangeOperation.RemoveUnusedBridge => "Remove unused bridge '{0}'",
                NetworkChangeOperation.AddBridge => "Add bridge '{0}'",
                NetworkChangeOperation.AddNetNat => "Add host NAT for provider '{0}'",
                NetworkChangeOperation.RemoveNetNat => "Remove host NAT for provider {0}",
                NetworkChangeOperation.RemoveAdapterPort => "Remove adapter '{0}' from bridge '{1}'",
                NetworkChangeOperation.AddAdapterPort => "Add adapter '{0}' to bridge '{1}'",
                NetworkChangeOperation.ConfigureNatIp => "Configure ip settings for NAT bridge '{0}'",
                NetworkChangeOperation.UpdateBridgeMapping => "Update mapping of bridges to network providers",
                NetworkChangeOperation.RemoveMissingBridge => "Host adapter '{0}' not found - removing bridge",
                _ => throw new ArgumentOutOfRangeException(nameof(key), key, null)
            };
        }

    }
}