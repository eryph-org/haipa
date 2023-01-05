﻿// <auto-generated />
using System;
using Eryph.StateDb;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Eryph.StateDb.Migrations
{
    [DbContext(typeof(StateStoreContext))]
    [Migration("20221216142219_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.11");

            modelBuilder.Entity("Eryph.StateDb.Model.Catlet", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AgentName")
                        .HasColumnType("TEXT");

                    b.Property<int>("CatletType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("TEXT");

                    b.Property<int>("ResourceType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<TimeSpan?>("UpTime")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Catlets");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Catlet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpAssignment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("IpAddress")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("NetworkPortId")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("SubnetId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("NetworkPortId");

                    b.HasIndex("SubnetId");

                    b.ToTable("IpAssignment");

                    b.HasDiscriminator<string>("Discriminator").HasValue("IpAssignment");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpPool", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Counter")
                        .HasColumnType("INTEGER");

                    b.Property<string>("FirstIp")
                        .HasColumnType("TEXT");

                    b.Property<string>("IpNetwork")
                        .HasColumnType("TEXT");

                    b.Property<string>("LastIp")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate()
                        .HasColumnType("BLOB");

                    b.Property<Guid>("SubnetId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("SubnetId");

                    b.ToTable("IpPools");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.NetworkPort", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("MacAddress")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProviderName")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("MacAddress")
                        .IsUnique();

                    b.ToTable("NetworkPorts");

                    b.HasDiscriminator<string>("Discriminator").HasValue("NetworkPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Operation", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StatusMessage")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Operations");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationLogEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("TaskId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.HasIndex("TaskId");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationResource", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ResourceId")
                        .HasColumnType("TEXT");

                    b.Property<int>("ResourceType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.ToTable("OperationResources");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationTask", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AgentName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.ToTable("OperationTasks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("TenantId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.HasIndex("TenantId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.ReportedNetwork", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("CatletId")
                        .HasColumnType("TEXT");

                    b.Property<string>("DnsServersInternal")
                        .HasColumnType("TEXT")
                        .HasColumnName("DnsServers");

                    b.Property<string>("IPv4DefaultGateway")
                        .HasColumnType("TEXT");

                    b.Property<string>("IPv6DefaultGateway")
                        .HasColumnType("TEXT");

                    b.Property<string>("IpV4AddressesInternal")
                        .HasColumnType("TEXT")
                        .HasColumnName("IpV4Addresses");

                    b.Property<string>("IpV4SubnetsInternal")
                        .HasColumnType("TEXT")
                        .HasColumnName("IpV4Subnets");

                    b.Property<string>("IpV6AddressesInternal")
                        .HasColumnType("TEXT")
                        .HasColumnName("IpV6Addresses");

                    b.Property<string>("IpV6SubnetsInternal")
                        .HasColumnType("TEXT")
                        .HasColumnName("IpV6Subnets");

                    b.HasKey("Id");

                    b.HasIndex("CatletId");

                    b.ToTable("ReportedNetworks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Subnet", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("IpNetwork")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Subnet");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Subnet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Tenant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Tenants");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualCatletNetworkAdapter", b =>
                {
                    b.Property<Guid>("MachineId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<string>("MacAddress")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("NetworkProviderName")
                        .HasColumnType("TEXT");

                    b.Property<string>("SwitchName")
                        .HasColumnType("TEXT");

                    b.HasKey("MachineId", "Id");

                    b.ToTable("VirtualCatletNetworkAdapters");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualDisk", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("DataStore")
                        .HasColumnType("TEXT");

                    b.Property<int>("DiskType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Environment")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("TEXT");

                    b.Property<int>("ResourceType")
                        .HasColumnType("INTEGER");

                    b.Property<long?>("SizeBytes")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StorageIdentifier")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("ParentId");

                    b.HasIndex("ProjectId");

                    b.ToTable("VirtualDisks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualMachineDrive", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("AttachedDiskId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("MachineId")
                        .HasColumnType("TEXT");

                    b.Property<int?>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AttachedDiskId");

                    b.HasIndex("MachineId");

                    b.ToTable("VirtualCatletDrives");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualMachineMetadata", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Metadata")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Metadata");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetwork", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("IpNetwork")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("NetworkProvider")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("TEXT");

                    b.Property<int>("ResourceType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("VirtualNetworks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.FloatingNetworkPort", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.NetworkPort");

                    b.Property<string>("PoolName")
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("PoolName");

                    b.Property<string>("SubnetName")
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("SubnetName");

                    b.HasDiscriminator().HasValue("FloatingNetworkPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpPoolAssignment", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.IpAssignment");

                    b.Property<int>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("PoolId")
                        .HasColumnType("TEXT");

                    b.HasIndex("PoolId");

                    b.HasDiscriminator().HasValue("IpPoolAssignment");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.ProviderSubnet", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Subnet");

                    b.Property<string>("ProviderName")
                        .HasColumnType("TEXT");

                    b.HasDiscriminator().HasValue("ProviderSubnet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualCatlet", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Catlet");

                    b.Property<Guid?>("HostId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("MetadataId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("VMId")
                        .HasColumnType("TEXT");

                    b.HasIndex("HostId");

                    b.HasDiscriminator().HasValue("VirtualCatlet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualCatletHost", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Catlet");

                    b.Property<string>("HardwareId")
                        .HasColumnType("TEXT");

                    b.HasDiscriminator().HasValue("VirtualCatletHost");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetworkPort", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.NetworkPort");

                    b.Property<Guid?>("FloatingPortId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("NetworkId")
                        .HasColumnType("TEXT");

                    b.HasIndex("FloatingPortId")
                        .IsUnique();

                    b.HasIndex("NetworkId");

                    b.HasDiscriminator().HasValue("VirtualNetworkPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetworkSubnet", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Subnet");

                    b.Property<int>("DhcpLeaseTime")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DnsServersV4")
                        .HasColumnType("TEXT");

                    b.Property<int>("MTU")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("NetworkId")
                        .HasColumnType("TEXT");

                    b.HasIndex("NetworkId");

                    b.HasDiscriminator().HasValue("VirtualNetworkSubnet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletNetworkPort", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.VirtualNetworkPort");

                    b.Property<Guid?>("CatletId")
                        .HasColumnType("TEXT");

                    b.HasIndex("CatletId");

                    b.HasDiscriminator().HasValue("CatletNetworkPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.NetworkRouterPort", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.VirtualNetworkPort");

                    b.Property<Guid>("RoutedNetworkId")
                        .HasColumnType("TEXT");

                    b.HasIndex("RoutedNetworkId")
                        .IsUnique();

                    b.HasDiscriminator().HasValue("NetworkRouterPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.ProviderRouterPort", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.VirtualNetworkPort");

                    b.Property<string>("PoolName")
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("PoolName");

                    b.Property<string>("SubnetName")
                        .ValueGeneratedOnUpdateSometimes()
                        .HasColumnType("TEXT")
                        .HasColumnName("SubnetName");

                    b.HasDiscriminator().HasValue("ProviderRouterPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Catlet", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Project", "Project")
                        .WithMany("Catlets")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpAssignment", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.NetworkPort", "NetworkPort")
                        .WithMany("IpAssignments")
                        .HasForeignKey("NetworkPortId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.HasOne("Eryph.StateDb.Model.Subnet", "Subnet")
                        .WithMany()
                        .HasForeignKey("SubnetId");

                    b.Navigation("NetworkPort");

                    b.Navigation("Subnet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpPool", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Subnet", "Subnet")
                        .WithMany("IpPools")
                        .HasForeignKey("SubnetId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Subnet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationLogEntry", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Operation", "Operation")
                        .WithMany("LogEntries")
                        .HasForeignKey("OperationId");

                    b.HasOne("Eryph.StateDb.Model.OperationTask", "Task")
                        .WithMany()
                        .HasForeignKey("TaskId");

                    b.Navigation("Operation");

                    b.Navigation("Task");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationResource", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Operation", "Operation")
                        .WithMany("Resources")
                        .HasForeignKey("OperationId");

                    b.Navigation("Operation");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationTask", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Operation", "Operation")
                        .WithMany("Tasks")
                        .HasForeignKey("OperationId");

                    b.Navigation("Operation");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Project", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Operation", null)
                        .WithMany("Projects")
                        .HasForeignKey("OperationId");

                    b.HasOne("Eryph.StateDb.Model.Tenant", "Tenant")
                        .WithMany("Projects")
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Tenant");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.ReportedNetwork", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Catlet", "Catlet")
                        .WithMany("ReportedNetworks")
                        .HasForeignKey("CatletId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Catlet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualCatletNetworkAdapter", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.VirtualCatlet", "Vm")
                        .WithMany("NetworkAdapters")
                        .HasForeignKey("MachineId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Vm");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualDisk", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.VirtualDisk", "Parent")
                        .WithMany("Childs")
                        .HasForeignKey("ParentId");

                    b.HasOne("Eryph.StateDb.Model.Project", "Project")
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Parent");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualMachineDrive", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.VirtualDisk", "AttachedDisk")
                        .WithMany("AttachedDrives")
                        .HasForeignKey("AttachedDiskId");

                    b.HasOne("Eryph.StateDb.Model.VirtualCatlet", "Vm")
                        .WithMany("Drives")
                        .HasForeignKey("MachineId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AttachedDisk");

                    b.Navigation("Vm");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetwork", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Project", "Project")
                        .WithMany("VirtualNetworks")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpPoolAssignment", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.IpPool", "Pool")
                        .WithMany("IpAssignments")
                        .HasForeignKey("PoolId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Pool");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualCatlet", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.VirtualCatletHost", "Host")
                        .WithMany("VirtualCatlets")
                        .HasForeignKey("HostId");

                    b.Navigation("Host");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetworkPort", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.FloatingNetworkPort", "FloatingPort")
                        .WithOne("AssignedPort")
                        .HasForeignKey("Eryph.StateDb.Model.VirtualNetworkPort", "FloatingPortId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.HasOne("Eryph.StateDb.Model.VirtualNetwork", "Network")
                        .WithMany("NetworkPorts")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("FloatingPort");

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetworkSubnet", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.VirtualNetwork", "Network")
                        .WithMany("Subnets")
                        .HasForeignKey("NetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Network");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletNetworkPort", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Catlet", "Catlet")
                        .WithMany("NetworkPorts")
                        .HasForeignKey("CatletId")
                        .OnDelete(DeleteBehavior.Cascade);

                    b.Navigation("Catlet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.NetworkRouterPort", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.VirtualNetwork", "RoutedNetwork")
                        .WithOne("RouterPort")
                        .HasForeignKey("Eryph.StateDb.Model.NetworkRouterPort", "RoutedNetworkId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("RoutedNetwork");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Catlet", b =>
                {
                    b.Navigation("NetworkPorts");

                    b.Navigation("ReportedNetworks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpPool", b =>
                {
                    b.Navigation("IpAssignments");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.NetworkPort", b =>
                {
                    b.Navigation("IpAssignments");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Operation", b =>
                {
                    b.Navigation("LogEntries");

                    b.Navigation("Projects");

                    b.Navigation("Resources");

                    b.Navigation("Tasks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Project", b =>
                {
                    b.Navigation("Catlets");

                    b.Navigation("VirtualNetworks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Subnet", b =>
                {
                    b.Navigation("IpPools");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Tenant", b =>
                {
                    b.Navigation("Projects");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualDisk", b =>
                {
                    b.Navigation("AttachedDrives");

                    b.Navigation("Childs");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetwork", b =>
                {
                    b.Navigation("NetworkPorts");

                    b.Navigation("RouterPort");

                    b.Navigation("Subnets");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.FloatingNetworkPort", b =>
                {
                    b.Navigation("AssignedPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualCatlet", b =>
                {
                    b.Navigation("Drives");

                    b.Navigation("NetworkAdapters");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualCatletHost", b =>
                {
                    b.Navigation("VirtualCatlets");
                });
#pragma warning restore 612, 618
        }
    }
}
