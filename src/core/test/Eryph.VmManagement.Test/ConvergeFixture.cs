﻿using Eryph.ConfigModel.Catlets;
using Eryph.Core.VmAgent;
using Eryph.Resources.Machines;
using Eryph.VmManagement.Converging;
using Eryph.VmManagement.Storage;

namespace Eryph.VmManagement.Test;

public class ConvergeFixture
{
    public ConvergeFixture()
    {
        var mapping = new FakeTypeMapping();
        Engine = new TestPowershellEngine(mapping);
        Config = new CatletConfig();
        StorageSettings = new VMStorageSettings();

        HostSettings = new HostSettings
        {
            DefaultDataPath = "x:\\data",
            DefaultNetwork = "default",
            DefaultVirtualHardDiskPath = "x:\\disks"
        };

        NetworkSettings = Array.Empty<MachineNetworkSettings>();
        HostInfo = new VMHostMachineData();
        VmHostAgentConfiguration = new VmHostAgentConfiguration()
        {
            Defaults = new()
            {
                Vms = "x:\\data",
                Volumes = "x:\\disks",
            },
        };
    }

    public VMStorageSettings StorageSettings { get; set; }


    public TestPowershellEngine Engine { get; }
    public MachineNetworkSettings[] NetworkSettings { get; set; }
    public CatletConfig Config { get; set; }
    public CatletMetadata Metadata { get; set; }

    public HostSettings HostSettings { get; set; }

    public VMHostMachineData HostInfo { get; set; }

    public VmHostAgentConfiguration VmHostAgentConfiguration { get; set; }


    public ConvergeContext Context =>
        new(VmHostAgentConfiguration, HostSettings, Engine, ReportProgressCallBack,
            Config, Metadata, StorageSettings, NetworkSettings, HostInfo);

    private static Task ReportProgressCallBack(string _) => Task.CompletedTask;

}