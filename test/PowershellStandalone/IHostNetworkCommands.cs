﻿using System.Net;
using Eryph.VmManagement;
using Eryph.VmManagement.Data.Core;
using Eryph.VmManagement.Data.Full;
using LanguageExt;
using LanguageExt.Effects.Traits;

namespace PowershellStandalone;

public interface IHostNetworkCommands<RT> where RT : struct, HasCancel<RT>
{
    Aff<RT, TypedPsObject<CimSession>> GetCimSession();
    Aff<RT, Unit> RemoveCimSession(TypedPsObject<CimSession> cimSession);

    Aff<RT, Seq<VMSwitch>> GetSwitches(TypedPsObject<CimSession> cimSession);
    Aff<RT, Seq<VMSwitchExtension>> GetSwitchExtensions(TypedPsObject<CimSession> cimSession);
    Aff<RT, Seq<HostNetworkAdapter>> GetPhysicalAdapters(TypedPsObject<CimSession> cimSession);
    Aff<RT, Seq<string>> GetAdapterNames(TypedPsObject<CimSession> cimSession);

    Aff<RT, Seq<NetNat>> GetNetNat(TypedPsObject<CimSession> cimSession);
    Aff<RT, Unit> EnableBridgeAdapter(string adapterName);
    Aff<RT, Unit> ConfigureNATAdapter(string adapterName, IPAddress ipAddress, IPNetwork network);
    Aff<RT, Seq<TypedPsObject<VMNetworkAdapter>>> GetNetAdaptersBySwitch(Guid switchId);
    Aff<RT, Unit> DisconnectNetworkAdapters(Seq<TypedPsObject<VMNetworkAdapter>> adapters);
    Aff<RT, Unit> ReconnectNetworkAdapters(Seq<TypedPsObject<VMNetworkAdapter>> adapters, string switchName);
    Aff<RT, Unit> CreateOverlaySwitch(IEnumerable<string> adapters);

    Aff<RT, Option<OverlaySwitchInfo>> FindOverlaySwitch(
        TypedPsObject<CimSession> cimSession,
        Seq<VMSwitch> vmSwitches,
        Seq<VMSwitchExtension> extensions, 
        Seq<HostNetworkAdapter> adapters);

    Aff<RT, Unit> RemoveOverlaySwitch();
    Aff<RT, Unit> RemoveNetNat(string natName);
    Aff<RT, Unit> WaitForBridgeAdapter(string bridgeName);
    Aff<RT,Unit> AddNetNat(string natName, IPNetwork network);

    Aff<RT, Seq<NetIpAddress>> GetAdapterIpV4Address(string adapterName);

}

public class CimSession
{

}