﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dbosoft.Rebus.Operations;
using Eryph.Core;
using Eryph.Messages.Resources.Catlets.Commands;
using Eryph.ModuleCore;
using Eryph.Modules.Controller.DataServices;
using Eryph.Rebus;
using Eryph.Resources.Disks;
using Eryph.Resources.Machines;
using Eryph.StateDb;
using Eryph.StateDb.Model;
using Eryph.StateDb.Specifications;
using LanguageExt;
using Rebus.Pipeline;

namespace Eryph.Modules.Controller.Inventory
{
    internal class UpdateInventoryCommandHandlerBase
    {
        private readonly IOperationDispatcher _dispatcher;
        private readonly IVirtualMachineDataService _vmDataService;
        private readonly IVirtualDiskDataService _vhdDataService;

        protected readonly IVirtualMachineMetadataService MetadataService;
        protected readonly IStateStore StateStore;
        private readonly IMessageContext _messageContext;

        protected UpdateInventoryCommandHandlerBase(
            IVirtualMachineMetadataService metadataService, IOperationDispatcher dispatcher,
            IVirtualMachineDataService vmDataService, 
            IVirtualDiskDataService vhdDataService,
            IStateStore stateStore, IMessageContext messageContext)
        {
            MetadataService = metadataService;
            _dispatcher = dispatcher;
            _vmDataService = vmDataService;
            _vhdDataService = vhdDataService;
            StateStore = stateStore;
            _messageContext = messageContext;
        }

        private static void SelectAllParentDisks(ref List<DiskInfo> parentDisks, DiskInfo disk)
        {
            if (disk.Parent != null)
                SelectAllParentDisks(ref parentDisks, disk.Parent);

            parentDisks.Add(disk);
        }

        protected async Task UpdateVMs(
            DateTimeOffset timestamp,
            IEnumerable<VirtualMachineData> vmList, CatletFarm hostMachine)
        {

            var vms = vmList as VirtualMachineData[] ?? vmList.ToArray();

            var diskInfos = vms.SelectMany(x => x.Drives.Select(d => d.Disk)).ToList();
            var allDisks = new List<DiskInfo>();
            foreach (var diskInfo in diskInfos) SelectAllParentDisks(ref allDisks, diskInfo);

            diskInfos = allDisks.Distinct((x, y) =>
                string.Equals(x.Path, y.Path, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.FileName, y.FileName, StringComparison.OrdinalIgnoreCase)).ToList();

            var addedDisks = new List<VirtualDisk>();

            foreach (var diskInfo in diskInfos)
            {
                var project = await FindProject(diskInfo.ProjectName, diskInfo.ProjectId)
                    .IfNoneAsync(() =>
                        FindRequiredProject("default", Guid.Empty)).ConfigureAwait(false);

                var disk = await LookupVirtualDisk(diskInfo, project, addedDisks)
                    .IfNoneAsync(async () =>
                    {
                    
                        var d = new VirtualDisk
                        {
                            Id = diskInfo.Id,
                            Name = diskInfo.Name,
                            DiskIdentifier = diskInfo.DiskIdentifier,
                            DataStore = diskInfo.DataStore,
                            Environment = diskInfo.Environment,
                            Geneset = diskInfo.Geneset,
                            StorageIdentifier = diskInfo.StorageIdentifier,
                            Project = project,
                            FileName = diskInfo.FileName,
                            Path = diskInfo.Path.ToLowerInvariant()

                        };
                        d = await _vhdDataService.AddNewVHD(d).ConfigureAwait(false);
                        addedDisks.Add(d);
                        return d;
                    }).ConfigureAwait(false);

                disk.SizeBytes = diskInfo.SizeBytes;
                disk.UsedSizeBytes = diskInfo.UsedSizeBytes;
                disk.Frozen = diskInfo.Frozen;
                disk.LastSeen = timestamp;
                disk.LastSeenAgent = hostMachine.Name;
                await _vhdDataService.UpdateVhd(disk).ConfigureAwait(false);

            }

            //second loop to assign parents and to update state db
            foreach (var diskInfo in diskInfos)
            {
                var project = await FindProject(diskInfo.ProjectName, diskInfo.ProjectId)
                    .IfNoneAsync(() =>
                        FindRequiredProject(EryphConstants.DefaultProjectName, Guid.Empty)).ConfigureAwait(false);

                await LookupVirtualDisk(diskInfo, project, addedDisks).IfSomeAsync(async currentDisk =>
                {
                    if (diskInfo.Parent == null)
                    {
                        currentDisk.Parent = null;
                        return;
                    }

                    await LookupVirtualDisk(diskInfo.Parent, project, addedDisks)
                        .IfSomeAsync(parentDisk =>
                        {
                            currentDisk.Parent = parentDisk;

                        }).ConfigureAwait(false);
                    await _vhdDataService.UpdateVhd(currentDisk).ConfigureAwait(false);

                }).ConfigureAwait(false);
            }


            foreach (var vmInfo in vms)
            {
                //get known metadata for VM, if metadata is unknown skip this VM as it is not in Eryph management
                var optionalMetadata = await MetadataService.GetMetadata(vmInfo.MetadataId).ConfigureAwait(false);
                //TODO: add logging that entry has been skipped due to missing metadata

                await optionalMetadata.IfSomeAsync(async metadata =>
                {
                    var optionalMachine = (await _vmDataService.GetVM(metadata.MachineId).ConfigureAwait(false));
                    var project = await FindRequiredProject(vmInfo.ProjectName, vmInfo.ProjectId).ConfigureAwait(false);

                    //machine not found or metadata is assigned to new VM - a new VM resource will be created)
                    if (optionalMachine.IsNone || metadata.VMId != vmInfo.VMId)
                    {
                        // create new metadata for machines that have been imported
                        if (metadata.VMId != vmInfo.VMId)
                        {
                            var oldMetadataId = metadata.Id;
                            metadata.Id = Guid.NewGuid();
                            metadata.MachineId = Guid.NewGuid();
                            metadata.VMId = vmInfo.VMId;
                            
                            await _dispatcher.StartNew(
                                project.TenantId,
                                _messageContext.GetTraceId(),
                                new UpdateCatletMetadataCommand
                                {
                                    AgentName = hostMachine.Name,
                                    CurrentMetadataId = oldMetadataId,
                                    NewMetadataId = metadata.Id,
                                    CatletId = metadata.MachineId,
                                    VMId = vmInfo.VMId,
                                }).ConfigureAwait(false);
                        }

                        if (metadata.MachineId == Guid.Empty)
                            metadata.MachineId = Guid.NewGuid();


                        var catlet = await VirtualMachineInfoToCatlet(vmInfo, 
                                hostMachine, metadata.MachineId, project, addedDisks)
                            .ConfigureAwait(false);
                        await _vmDataService.AddNewVM(catlet, metadata).ConfigureAwait(false);

                        return;
                    }

                    await optionalMachine.IfSomeAsync(async existingMachine =>
                    {
                        await StateStore.LoadPropertyAsync(existingMachine, x=> x.Project).ConfigureAwait(false);

                        Debug.Assert(existingMachine.Project != null);

                        await StateStore.LoadCollectionAsync(existingMachine, x => x.ReportedNetworks).ConfigureAwait(false);
                        await StateStore.LoadCollectionAsync(existingMachine, x => x.NetworkAdapters).ConfigureAwait(false);


                        // update data for existing machine
                        var newMachine = await VirtualMachineInfoToCatlet(vmInfo,
                            hostMachine, existingMachine.Id, existingMachine.Project, addedDisks).ConfigureAwait(false);
                        existingMachine.Name = newMachine.Name;
                        existingMachine.Status = newMachine.Status;
                        existingMachine.Host = hostMachine;
                        existingMachine.AgentName = newMachine.AgentName;
                        existingMachine.Frozen = newMachine.Frozen;
                        existingMachine.DataStore = newMachine.DataStore;
                        existingMachine.Environment = newMachine.Environment;
                        existingMachine.Path = newMachine.Path;
                        existingMachine.StorageIdentifier = newMachine.StorageIdentifier;
                        existingMachine.ReportedNetworks = newMachine.ReportedNetworks;
                        existingMachine.NetworkAdapters = newMachine.NetworkAdapters;
                        existingMachine.Drives = newMachine.Drives;
                        existingMachine.CpuCount = newMachine.CpuCount;
                        existingMachine.StartupMemory = newMachine.StartupMemory;
                        existingMachine.MinimumMemory = newMachine.MinimumMemory;
                        existingMachine.StartupMemory = newMachine.StartupMemory;
                        existingMachine.Features = newMachine.Features;
                        existingMachine.SecureBootTemplate = newMachine.SecureBootTemplate;
                    }).ConfigureAwait(false);


                }).ConfigureAwait(false);
            }
        }

        private async Task<Option<VirtualDisk>> LookupVirtualDisk(DiskInfo diskInfo, Project project,
            IReadOnlyCollection<VirtualDisk> addedDisks)
        {

            return await _vhdDataService.FindVHDByLocation(
                    project.Id,
                    diskInfo.DataStore,
                    diskInfo.Environment,
                    diskInfo.StorageIdentifier,
                    diskInfo.Name,
                    diskInfo.DiskIdentifier)
                .Map(l => addedDisks.Append(l))
                .Map(l => l.Filter(
                    x => x.DataStore == diskInfo.DataStore &&
                         x.Project.Name == diskInfo.ProjectName &&
                         x.Environment == diskInfo.Environment &&
                         x.StorageIdentifier == diskInfo.StorageIdentifier &&
                         x.Name == diskInfo.Name))
                .Map(x => x.ToArray())
                .Map(candidates => candidates.Length <= 1
                    ? candidates.HeadOrNone()
                    : candidates.Find(x =>
                        string.Equals(x.Path, diskInfo.Path, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(x.FileName, diskInfo.FileName, StringComparison.OrdinalIgnoreCase))).ConfigureAwait(false);
        }

        protected async Task<Option<Project>> FindProject(
            string projectName, Guid? optionalProjectId)
        {
            if (optionalProjectId.GetValueOrDefault() != Guid.Empty)
                return await StateStore.For<Project>().GetByIdAsync(optionalProjectId.GetValueOrDefault()).ConfigureAwait(false);

            if(string.IsNullOrWhiteSpace(projectName))
                projectName = "default";

            return await StateStore.For<Project>()
                .GetBySpecAsync(new ProjectSpecs.GetByName(
                    EryphConstants.DefaultTenantId, projectName)).ConfigureAwait(false);
        }

        protected async Task<Project> FindRequiredProject(string projectName,
            Guid? projectId)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                projectName = "default";

            var foundProject = await FindProject(projectName, projectId).ConfigureAwait(false);

            if(foundProject.IsNone && !projectId.HasValue)
                throw new NotFoundException($"Project '{projectName}' not found.");

            if (foundProject.IsNone && projectId.HasValue)
                throw new NotFoundException($"Project '{projectId}' not found.");

            return foundProject.IfNone(new Project());
        }

        private Task<Catlet> VirtualMachineInfoToCatlet(VirtualMachineData vmInfo, CatletFarm hostMachine,
            Guid machineId, Project project, IReadOnlyCollection<VirtualDisk> addedDisks)
        {
            return
                from drivesAndDisks in Task.FromResult(vmInfo.Drives)
                    .MapAsync( drives=> drives.Map(d =>
                        d.Disk != null
                            ? LookupVirtualDisk(d.Disk, project, addedDisks)
                                .Map(disk=>(Drive: d, Disk: disk ))
                            : Task.FromResult( (Drive: d, Disk: Option<VirtualDisk>.None))
                        ).TraverseSerial(l=>l))

                 let drives = drivesAndDisks.Map(d => new CatletDrive
                 {
                     Id = d.Drive.Id,
                     CatletId = machineId,
                     Type = d.Drive.Type,
                     AttachedDisk = d.Disk.IfNoneUnsafe(() => null)

                }).ToList()
                
                select new Catlet
                {
                Id = machineId,
                Project = project,
                ProjectId = project.Id,
                VMId = vmInfo.VMId,
                Name = vmInfo.Name,
                Status = MapVmStatusToMachineStatus(vmInfo.Status),
                Host = hostMachine,
                AgentName = hostMachine.Name,
                DataStore = vmInfo.DataStore,
                Environment = vmInfo.Environment,
                Path = vmInfo.VMPath,
                Frozen = vmInfo.Frozen,
                StorageIdentifier = vmInfo.StorageIdentifier,
                MetadataId = vmInfo.MetadataId,
                UpTime = vmInfo.UpTime,
                CpuCount = vmInfo.Cpu?.Count ?? 0,
                StartupMemory = vmInfo.Memory?.Startup ?? 0,
                MinimumMemory = vmInfo.Memory?.Minimum ?? 0,
                MaximumMemory = vmInfo.Memory?.Startup ?? 0,
                Features = MapFeatures(vmInfo),
                SecureBootTemplate = vmInfo.Firmware?.SecureBootTemplate,
                NetworkAdapters = vmInfo.NetworkAdapters.Select(a => new CatletNetworkAdapter
                {
                    Id = a.Id,
                    CatletId = machineId,
                    Name = a.AdapterName,
                    SwitchName = a.VirtualSwitchName
                }).ToList(),
                Drives = drives,
                ReportedNetworks = (vmInfo.Networks?.ToReportedNetwork(machineId) ?? Array.Empty<ReportedNetwork>()).ToList()
            };
        }

        private static List<CatletFeature> MapFeatures(VirtualMachineData vmInfo)
        {
            var features = new List<CatletFeature>();
            
            if(vmInfo.Firmware?.SecureBoot ?? false)
                features.Add(CatletFeature.SecureBoot);
            if (vmInfo.Cpu?.ExposeVirtualizationExtensions ?? false)
                features.Add(CatletFeature.NestedVirtualization);


            return features;
        }

        private static CatletStatus MapVmStatusToMachineStatus(VmStatus status)
        {
            switch (status)
            {
                case VmStatus.Stopped:
                    return CatletStatus.Stopped;
                case VmStatus.Running:
                    return CatletStatus.Running;
                case VmStatus.Pending:
                    return CatletStatus.Pending;
                case VmStatus.Error:
                    return CatletStatus.Error;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

    }
}