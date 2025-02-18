﻿using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dbosoft.OVN.Model;
using Eryph.Core;
using Eryph.Modules.VmHostAgent.Networks;
using Eryph.Modules.VmHostAgent.Networks.OVS;
using Eryph.VmManagement.Data.Core;
using Eryph.VmManagement.Data.Full;
using Moq;

using static Eryph.Modules.VmHostAgent.Networks.HostStateProvider<Eryph.Modules.VmHostAgent.Test.TestRuntime>;
using static LanguageExt.Prelude;

namespace Eryph.Modules.VmHostAgent.Test.Networks;

public class HostStateProviderTests
{
    private readonly Mock<IOVSControl> _ovsControlMock = new();
    private readonly Mock<INetworkProviderManager> _networkProviderManagerMock = new();
    private readonly Mock<IHostNetworkCommands<TestRuntime>> _hostNetworkCommandsMock = new();
    private readonly Mock<ISyncClient> _syncClientMock = new();
    private readonly TestRuntime _runtime;

    public HostStateProviderTests()
    {
        _runtime = TestRuntime.New(
            _ovsControlMock.Object,
            _syncClientMock.Object,
            _hostNetworkCommandsMock.Object,
            _networkProviderManagerMock.Object);
    }

    [Fact]
    public async Task GetHostState_ComplexHostState_ReturnsExpectedData()
    {
        var switchId = Guid.NewGuid();
        var switchExtensionId = Guid.NewGuid().ToString();
        var pif1Id = Guid.NewGuid();
        var pif2Id = Guid.NewGuid();
        var otherAdapterId = Guid.NewGuid();

        _hostNetworkCommandsMock.Setup(x => x.GetSwitchExtensions())
            .Returns(SuccessAff(Seq1(new VMSwitchExtension
            {
                Enabled = true,
                Id = switchExtensionId, 
                SwitchId = switchId,
                SwitchName = EryphConstants.OverlaySwitchName
            })));

        _hostNetworkCommandsMock.Setup(x => x.GetSwitches())
            .Returns(SuccessAff(Seq1(new VMSwitch
            {
                Id = switchId,
                Name = EryphConstants.OverlaySwitchName,
                NetAdapterInterfaceGuid = [pif1Id, pif2Id],
            })));

        _hostNetworkCommandsMock.Setup(x => x.GetHostAdapters())
            .Returns(SuccessAff(Seq(
                new HostNetworkAdapter
                {
                    InterfaceGuid = pif1Id,
                    Name = "pif-1",
                    Virtual = false,
                },
                new HostNetworkAdapter
                {
                    InterfaceGuid = pif2Id,
                    Name = "pif-2",
                    Virtual = false,
                },
                new HostNetworkAdapter
                {
                    InterfaceGuid = otherAdapterId,
                    Name = "other-adapter",
                    Virtual = true,
                })));

        _hostNetworkCommandsMock.Setup(x => x.GetNetNat())
            .Returns(SuccessAff(Seq1(new NetNat
            {
                Name = "test-nat",
                InternalIPInterfaceAddressPrefix = "10.0.0.0/24",
            })));

        var brIntId = Guid.NewGuid();
        var brIntPort1Id = Guid.NewGuid();
        var brIntPort1Interface1Id = Guid.NewGuid();

        var brPifId = Guid.NewGuid();
        var brPifPort1Id = Guid.NewGuid();
        var brPifPort1Interface1Id = Guid.NewGuid();
        var brPifPort2Id = Guid.NewGuid();
        var brPifPort2Interface1Id = Guid.NewGuid();
        var brPifPort2Interface2Id = Guid.NewGuid();

        _ovsControlMock.Setup(x => x.GetBridges(It.IsAny<CancellationToken>()))
            .Returns(Seq(
                OVSEntity.FromValueMap<Bridge>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brIntId)),
                    ("name", OVSValue<string>.New("br-int")),
                    ("ports", OVSReference.New(Seq1(brIntPort1Id))))),
                OVSEntity.FromValueMap<Bridge>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brPifId)),
                    ("name", OVSValue<string>.New("br-pif")),
                    ("ports", OVSReference.New(Seq(brPifPort1Id, brPifPort2Id)))))
                ));

        _ovsControlMock.Setup(x => x.GetPorts(It.IsAny<CancellationToken>()))
            .Returns(Seq(
                OVSEntity.FromValueMap<BridgePort>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brIntPort1Id)),
                    ("name", OVSValue<string>.New("br-int")),
                    ("interfaces", OVSReference.New(Seq1(brIntPort1Interface1Id))))),
                OVSEntity.FromValueMap<BridgePort>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brPifPort1Id)),
                    ("name", OVSValue<string>.New("br-pif")),
                    ("interfaces", OVSReference.New(Seq1(brPifPort1Interface1Id))))),
                OVSEntity.FromValueMap<BridgePort>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brPifPort2Id)),
                    ("name", OVSValue<string>.New("br-pif-bond")),
                    ("interfaces", OVSReference.New(Seq(brPifPort2Interface1Id, brPifPort2Interface2Id)))))
                ));

        _ovsControlMock.Setup(x => x.GetInterfaces(It.IsAny<CancellationToken>()))
            .Returns(Seq(
                OVSEntity.FromValueMap<Interface>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brIntPort1Interface1Id)),
                    ("name", OVSValue<string>.New("br-int")),
                    ("type", OVSValue<string>.New("internal")))),
                OVSEntity.FromValueMap<Interface>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brPifPort1Interface1Id)),
                    ("name", OVSValue<string>.New("br-pif")),
                    ("type", OVSValue<string>.New("internal")))),
                OVSEntity.FromValueMap<Interface>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brPifPort2Interface1Id)),
                    ("name", OVSValue<string>.New("pif-1")),
                    ("type", OVSValue<string>.New("")),
                    ("external_ids", OVSMap<string>.New(Map(
                        ("host-iface-id", pif1Id.ToString())
                    ))))),
                OVSEntity.FromValueMap<Interface>(Map<string, IOVSField>(
                    ("_uuid", OVSValue<Guid>.New(brPifPort2Interface2Id)),
                    ("name", OVSValue<string>.New("pif-2")),
                    ("type", OVSValue<string>.New("")),
                    ("external_ids", OVSMap<string>.New(Map(
                        ("host-iface-id", pif2Id.ToString())
                    )))))
            ));

        var result = await getHostState().Run(_runtime);

        var hostState = result.Should().BeSuccess().Subject;
        hostState.VMSwitches.Should().SatisfyRespectively(
            vmSwitch =>
            {
                vmSwitch.Id.Should().Be(switchId);
                vmSwitch.Name.Should().Be(EryphConstants.OverlaySwitchName);
                vmSwitch.NetAdapterInterfaceGuid.Should().Equal(pif1Id, pif2Id);
            });
        
        hostState.VMSwitchExtensions.Should().SatisfyRespectively(
            vmSwitchExtension =>
            {
                vmSwitchExtension.Id.Should().Be(switchExtensionId);
                vmSwitchExtension.Enabled.Should().BeTrue();
                vmSwitchExtension.SwitchId.Should().Be(switchId);
                vmSwitchExtension.SwitchName.Should().Be(EryphConstants.OverlaySwitchName);
            });

        var pif1Info = hostState.HostAdapters.Adapters.ToDictionary().Should().ContainKey("pif-1").WhoseValue;
        pif1Info.InterfaceId.Should().Be(pif1Id);
        pif1Info.Name.Should().Be("pif-1");
        pif1Info.IsPhysical.Should().BeTrue();

        var pif2Info = hostState.HostAdapters.Adapters.ToDictionary().Should().ContainKey("pif-2").WhoseValue;
        pif2Info.InterfaceId.Should().Be(pif2Id);
        pif2Info.Name.Should().Be("pif-2");
        pif2Info.IsPhysical.Should().BeTrue();

        var otherAdapterInfo = hostState.HostAdapters.Adapters.ToDictionary().Should().ContainKey("other-adapter").WhoseValue;
        otherAdapterInfo.InterfaceId.Should().Be(otherAdapterId);
        otherAdapterInfo.Name.Should().Be("otherAdapter");
        otherAdapterInfo.IsPhysical.Should().BeFalse();

        var overlaySwítchInfo = hostState.OverlaySwitch.Should().BeSome().Subject;
        overlaySwítchInfo.Id.Should().Be(switchId);
        overlaySwítchInfo.AdaptersInSwitch.Should().Equal("pif-1", "pif-2");

        var brIntInfo = hostState.OvsBridges.Bridges.ToDictionary().Should().ContainKey("br-int").WhoseValue;
        brIntInfo.Name.Should().Be("br-int");
        
        var brIntPort1Info = brIntInfo.Ports.ToDictionary().Should().ContainKey("br-int").WhoseValue;
        brIntPort1Info.PortName.Should().Be("br-int");
        brIntPort1Info.BridgeName.Should().Be("br-int");
        brIntPort1Info.Interfaces.Should().SatisfyRespectively(
            i =>
            {
                i.Name.Should().Be("br-int");
                i.Type.Should().Be("internal");
                i.IsExternal.Should().BeFalse();
                i.HostInterfaceId.Should().BeNone();
            });

        var brPifInfo = hostState.OvsBridges.Bridges.ToDictionary().Should().ContainKey("br-pif").WhoseValue;
        brPifInfo.Name.Should().Be("br-pif");

        var brPifPort1Info = brPifInfo.Ports.ToDictionary().Should().ContainKey("br-pif").WhoseValue;
        brPifPort1Info.PortName.Should().Be("br-pif");
        brPifPort1Info.BridgeName.Should().Be("br-pif");
        brPifPort1Info.Interfaces.Should().SatisfyRespectively(
            i =>
            {
                i.Name.Should().Be("br-pif");
                i.Type.Should().Be("internal");
                i.IsExternal.Should().BeFalse();
                i.HostInterfaceId.Should().BeNone();
            });

        var brPifPort2Info = brPifInfo.Ports.ToDictionary().Should().ContainKey("br-pif-bond").WhoseValue;
        brPifPort2Info.PortName.Should().Be("br-pif-bond");
        brPifPort2Info.BridgeName.Should().Be("br-pif");
        brPifPort2Info.Interfaces.Should().SatisfyRespectively(
            i =>
            {
                i.Name.Should().Be("pif-1");
                i.Type.Should().Be("");
                i.IsExternal.Should().BeTrue();
                i.HostInterfaceId.Should().Be(pif1Id);
            },
            i =>
            {
                i.Name.Should().Be("pif-2");
                i.Type.Should().Be("");
                i.IsExternal.Should().BeTrue();
                i.HostInterfaceId.Should().Be(pif2Id);
            });
    }
}