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
    [Migration("20240428115826_diskdata4")]
    partial class diskdata4
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("Eryph.StateDb.Model.CatletDrive", b =>
                {
                    b.Property<string>("Id")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("AttachedDiskId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("CatletId")
                        .HasColumnType("TEXT");

                    b.Property<int?>("Type")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("AttachedDiskId");

                    b.HasIndex("CatletId");

                    b.ToTable("CatletDrives", (string)null);
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletMetadata", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Metadata")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Metadata");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletNetworkAdapter", b =>
                {
                    b.Property<Guid>("CatletId")
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

                    b.HasKey("CatletId", "Id");

                    b.ToTable("CatletNetworkAdapters");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpAssignment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(21)
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

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpPool", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("FirstIp")
                        .HasColumnType("TEXT");

                    b.Property<string>("IpNetwork")
                        .HasColumnType("TEXT");

                    b.Property<string>("LastIp")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<string>("NextIp")
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
                        .HasMaxLength(21)
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

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationLogEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Message")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("TaskId")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.HasIndex("TaskId");

                    b.ToTable("Logs");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationModel", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("LastUpdated")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StatusMessage")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("TenantId")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT")
                        .HasDefaultValue(new Guid("c1813384-8ecb-4f17-b846-821ee515d19b"));

                    b.HasKey("Id");

                    b.ToTable("Operations");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationProjectModel", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.HasIndex("ProjectId");

                    b.ToTable("OperationProjectModel");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationResourceModel", b =>
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

            modelBuilder.Entity("Eryph.StateDb.Model.OperationTaskModel", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("AgentName")
                        .HasColumnType("TEXT");

                    b.Property<string>("DisplayName")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("LastUpdated")
                        .IsConcurrencyToken()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ParentTaskId")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("ProjectId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ReferenceId")
                        .HasColumnType("TEXT");

                    b.Property<string>("ReferenceProjectName")
                        .HasColumnType("TEXT");

                    b.Property<int?>("ReferenceType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.HasIndex("ProjectId");

                    b.ToTable("OperationTasks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Project", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("TenantId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("TenantId");

                    b.ToTable("Projects");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.ProjectRoleAssignment", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("IdentityId")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasAlternateKey("ProjectId", "IdentityId", "RoleId");

                    b.ToTable("ProjectRoles");
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

            modelBuilder.Entity("Eryph.StateDb.Model.Resource", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("ProjectId")
                        .HasColumnType("TEXT");

                    b.Property<int>("ResourceType")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("ProjectId");

                    b.ToTable("Resources", (string)null);

                    b.UseTptMappingStrategy();
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Subnet", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasMaxLength(21)
                        .HasColumnType("TEXT");

                    b.Property<string>("IpNetwork")
                        .HasColumnType("TEXT");

                    b.Property<string>("Name")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Subnet");

                    b.HasDiscriminator<string>("Discriminator").HasValue("Subnet");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("Eryph.StateDb.Model.TaskProgressEntry", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.Property<Guid>("OperationId")
                        .HasColumnType("TEXT");

                    b.Property<int>("Progress")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("TaskId")
                        .HasColumnType("TEXT");

                    b.Property<DateTimeOffset>("Timestamp")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.HasIndex("TaskId");

                    b.ToTable("TaskProgress");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Tenant", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("Tenants");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.IpPoolAssignment", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.IpAssignment");

                    b.Property<int>("Number")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("PoolId")
                        .HasColumnType("TEXT");

                    b.HasIndex("PoolId", "Number")
                        .IsUnique();

                    b.HasDiscriminator().HasValue("IpPoolAssignment");
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

            modelBuilder.Entity("Eryph.StateDb.Model.Catlet", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Resource");

                    b.Property<string>("AgentName")
                        .HasColumnType("TEXT");

                    b.Property<int>("CatletType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("CpuCount")
                        .HasColumnType("INTEGER");

                    b.Property<string>("DataStore")
                        .HasColumnType("TEXT");

                    b.Property<string>("Environment")
                        .HasColumnType("TEXT");

                    b.Property<string>("Features")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Frozen")
                        .HasColumnType("INTEGER");

                    b.Property<Guid?>("HostId")
                        .HasColumnType("TEXT");

                    b.Property<long>("MaximumMemory")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("MetadataId")
                        .HasColumnType("TEXT");

                    b.Property<long>("MinimumMemory")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<string>("SecureBootTemplate")
                        .HasColumnType("TEXT");

                    b.Property<long>("StartupMemory")
                        .HasColumnType("INTEGER");

                    b.Property<int>("Status")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("StatusTimestamp")
                        .HasColumnType("TEXT");

                    b.Property<string>("StorageIdentifier")
                        .HasColumnType("TEXT");

                    b.Property<TimeSpan?>("UpTime")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("VMId")
                        .HasColumnType("TEXT");

                    b.HasIndex("HostId");

                    b.ToTable("Catlets", (string)null);
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletFarm", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Resource");

                    b.Property<string>("HardwareId")
                        .HasColumnType("TEXT");

                    b.ToTable("CatletFarms", (string)null);
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualDisk", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Resource");

                    b.Property<string>("DataStore")
                        .HasColumnType("TEXT");

                    b.Property<Guid>("DiskIdentifier")
                        .HasColumnType("TEXT");

                    b.Property<int>("DiskType")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Environment")
                        .HasColumnType("TEXT");

                    b.Property<string>("FileName")
                        .HasColumnType("TEXT");

                    b.Property<bool>("Frozen")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Geneset")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("LastSeen")
                        .HasColumnType("TEXT");

                    b.Property<string>("LastSeenAgent")
                        .HasColumnType("TEXT");

                    b.Property<Guid?>("ParentId")
                        .HasColumnType("TEXT");

                    b.Property<string>("Path")
                        .HasColumnType("TEXT");

                    b.Property<long?>("SizeBytes")
                        .HasColumnType("INTEGER");

                    b.Property<string>("StorageIdentifier")
                        .HasColumnType("TEXT");

                    b.Property<long?>("UsedSizeBytes")
                        .HasColumnType("INTEGER");

                    b.HasIndex("ParentId");

                    b.ToTable("CatletDisks", (string)null);
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetwork", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Resource");

                    b.Property<string>("Environment")
                        .HasColumnType("TEXT");

                    b.Property<string>("IpNetwork")
                        .HasColumnType("TEXT");

                    b.Property<string>("NetworkProvider")
                        .HasColumnType("TEXT");

                    b.ToTable("VNetworks", (string)null);
                });

            modelBuilder.Entity("Eryph.StateDb.Model.ProviderSubnet", b =>
                {
                    b.HasBaseType("Eryph.StateDb.Model.Subnet");

                    b.Property<string>("ProviderName")
                        .HasColumnType("TEXT");

                    b.HasDiscriminator().HasValue("ProviderSubnet");
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

                    b.Property<Guid>("CatletMetadataId")
                        .HasColumnType("TEXT");

                    b.HasIndex("CatletMetadataId");

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

            modelBuilder.Entity("Eryph.StateDb.Model.CatletDrive", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.VirtualDisk", "AttachedDisk")
                        .WithMany("AttachedDrives")
                        .HasForeignKey("AttachedDiskId");

                    b.HasOne("Eryph.StateDb.Model.Catlet", "Catlet")
                        .WithMany("Drives")
                        .HasForeignKey("CatletId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("AttachedDisk");

                    b.Navigation("Catlet");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletNetworkAdapter", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Catlet", "Catlet")
                        .WithMany("NetworkAdapters")
                        .HasForeignKey("CatletId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Catlet");
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
                    b.HasOne("Eryph.StateDb.Model.OperationModel", "Operation")
                        .WithMany("LogEntries")
                        .HasForeignKey("OperationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Eryph.StateDb.Model.OperationTaskModel", "Task")
                        .WithMany()
                        .HasForeignKey("TaskId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Operation");

                    b.Navigation("Task");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationProjectModel", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.OperationModel", "Operation")
                        .WithMany("Projects")
                        .HasForeignKey("OperationId");

                    b.HasOne("Eryph.StateDb.Model.Project", "Project")
                        .WithMany()
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Operation");

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationResourceModel", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.OperationModel", "Operation")
                        .WithMany("Resources")
                        .HasForeignKey("OperationId");

                    b.Navigation("Operation");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationTaskModel", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.OperationModel", "Operation")
                        .WithMany("Tasks")
                        .HasForeignKey("OperationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Eryph.StateDb.Model.Project", null)
                        .WithMany("ReferencedTasks")
                        .HasForeignKey("ProjectId");

                    b.Navigation("Operation");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Project", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Tenant", "Tenant")
                        .WithMany("Projects")
                        .HasForeignKey("TenantId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Tenant");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.ProjectRoleAssignment", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Project", "Project")
                        .WithMany("ProjectRoles")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Project");
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

            modelBuilder.Entity("Eryph.StateDb.Model.Resource", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Project", "Project")
                        .WithMany("Resources")
                        .HasForeignKey("ProjectId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();

                    b.Navigation("Project");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.TaskProgressEntry", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.OperationTaskModel", "Task")
                        .WithMany("Progress")
                        .HasForeignKey("TaskId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Task");
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

            modelBuilder.Entity("Eryph.StateDb.Model.Catlet", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.CatletFarm", "Host")
                        .WithMany("Catlets")
                        .HasForeignKey("HostId");

                    b.HasOne("Eryph.StateDb.Model.Resource", null)
                        .WithOne()
                        .HasForeignKey("Eryph.StateDb.Model.Catlet", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Host");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletFarm", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Resource", null)
                        .WithOne()
                        .HasForeignKey("Eryph.StateDb.Model.CatletFarm", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualDisk", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Resource", null)
                        .WithOne()
                        .HasForeignKey("Eryph.StateDb.Model.VirtualDisk", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Eryph.StateDb.Model.VirtualDisk", "Parent")
                        .WithMany("Childs")
                        .HasForeignKey("ParentId");

                    b.Navigation("Parent");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.VirtualNetwork", b =>
                {
                    b.HasOne("Eryph.StateDb.Model.Resource", null)
                        .WithOne()
                        .HasForeignKey("Eryph.StateDb.Model.VirtualNetwork", "Id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
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
                    b.HasOne("Eryph.StateDb.Model.CatletMetadata", null)
                        .WithMany()
                        .HasForeignKey("CatletMetadataId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
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

            modelBuilder.Entity("Eryph.StateDb.Model.IpPool", b =>
                {
                    b.Navigation("IpAssignments");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.NetworkPort", b =>
                {
                    b.Navigation("IpAssignments");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationModel", b =>
                {
                    b.Navigation("LogEntries");

                    b.Navigation("Projects");

                    b.Navigation("Resources");

                    b.Navigation("Tasks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.OperationTaskModel", b =>
                {
                    b.Navigation("Progress");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Project", b =>
                {
                    b.Navigation("ProjectRoles");

                    b.Navigation("ReferencedTasks");

                    b.Navigation("Resources");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Subnet", b =>
                {
                    b.Navigation("IpPools");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Tenant", b =>
                {
                    b.Navigation("Projects");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.FloatingNetworkPort", b =>
                {
                    b.Navigation("AssignedPort");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.Catlet", b =>
                {
                    b.Navigation("Drives");

                    b.Navigation("NetworkAdapters");

                    b.Navigation("ReportedNetworks");
                });

            modelBuilder.Entity("Eryph.StateDb.Model.CatletFarm", b =>
                {
                    b.Navigation("Catlets");
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
#pragma warning restore 612, 618
        }
    }
}
