﻿using Eryph.ConfigModel.Catlets;
using Eryph.ConfigModel.Variables;
using Eryph.Core.Genetics;

namespace Eryph.Core.Tests.Genetics;

public class CatletBreedingTests
{
    [Fact]
    public void Naming_and_placement_is_taken_from_child()
    {
        var parent = new CatletConfig
        {
            Name = "parent",
            Parent = "dbosoft/grandparent/1.0",
            Version = "parent-version",
            Project = "parent-project",
            Location = "parent-location",
            Environment = "parent-environment",
            Store = "parent-store",
            Hostname = "parent-hostname",
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/parent/1.0",
            Version = "child-version",
            Project = "child-project",
            Location = "child-location",
            Environment = "child-environment",
            Store = "child-store",
            Hostname = "child-hostname",
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Name.Should().Be("child");
        breedChild.Parent.Should().Be("dbosoft/parent/1.0");
        breedChild.Version.Should().Be("child-version");
        breedChild.Project.Should().Be("child-project");
        breedChild.Location.Should().Be("child-location");
        breedChild.Environment.Should().Be("child-environment");
        breedChild.Store.Should().Be("child-store");
        breedChild.Hostname.Should().Be("child-hostname");
    }

    [Fact]
    public void Child_get_parent_attributes()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Capabilities =
            [
                new CatletCapabilityConfig
                {
                    Name = "Cap1"
                }
            ],
            Cpu = new CatletCpuConfig { Count = 2 },
            Drives =
            [
                new CatletDriveConfig
                {
                    Name = "sda",
                    Type = CatletDriveType.VHD,
                    Source = "gene:dbosoft/test/1.0:sda",
                    Store = "lair",
                    Size = 100
                }
            ],
            Memory = new CatletMemoryConfig { Startup = 2048 },
            NetworkAdapters =
            [
                new CatletNetworkAdapterConfig
                {
                    Name = "eth0"
                }
            ],
            Networks =
            [
                new CatletNetworkConfig
                {
                    Name = "default",
                    AdapterName = "eth0",
                    SubnetV4 = new CatletSubnetConfig
                    {
                        Name = "sub1",
                        IpPool = "pool1"
                    }
                }
            ],
            Fodder =
            [
                new FodderConfig
                {
                    Source = "gene:dbosoft/test-fodder/1.0:test-fodder"
                }
            ],
            Variables =
            [
                new VariableConfig() { Name = "parentCatletVariable" }
            ],
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Project = "social",
            Environment = "env1",
            Drives = [],
            Networks =
            [
                new CatletNetworkConfig
                {
                    Name = "default",
                    AdapterName = "eth0"
                }
            ],
            Fodder =
            [
                new FodderConfig { Name = "food" }
            ],
            Variables =
            [
                new VariableConfig() { Name = "catletVariable" }
            ]
        };

        CatletBreeding.Breed(parent, child).IfLeft(e => e.Throw());

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Parent.Should().Be("dbosoft/test/1.0");
        breedChild.Project.Should().Be("social");
        breedChild.Environment.Should().Be("env1");

        breedChild.Capabilities.Should().NotBeNull();
        breedChild.Capabilities.Should().BeEquivalentTo(parent.Capabilities);
        breedChild.Capabilities.Should().NotBeSameAs(parent.Capabilities);

        breedChild.Drives.Should().NotBeNull();
        breedChild.Drives.Should().HaveCount(1);
        breedChild.Drives.Should().NotBeEquivalentTo(parent.Drives);
        breedChild.Drives?[0].Source.Should().Be("gene:dbosoft/test/1.0:sda");
        breedChild.Drives.Should().NotBeSameAs(parent.Drives);

        breedChild.NetworkAdapters.Should().NotBeNull();
        breedChild.NetworkAdapters.Should().HaveCount(1);
        breedChild.NetworkAdapters.Should().BeEquivalentTo(parent.NetworkAdapters);
        breedChild.NetworkAdapters.Should().NotBeSameAs(parent.NetworkAdapters);

        breedChild.Networks.Should().NotBeNull();
        breedChild.Networks.Should().HaveCount(1);
        breedChild.Networks.Should().BeEquivalentTo(parent.Networks);
        breedChild.Networks.Should().NotBeSameAs(parent.Networks);

        breedChild.Fodder.Should().NotBeNull();
        breedChild.Fodder.Should().HaveCount(2);
        breedChild.Fodder?[0].Source.Should().Be("gene:dbosoft/test-fodder/1.0:test-fodder");

        breedChild.Variables.Should().SatisfyRespectively(
            variable => variable.Name.Should().Be("catletVariable"),
            variable => variable.Name.Should().Be("parentCatletVariable"));
    }

    [Fact]
    public void Capabilities_are_merged()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Capabilities =
            [
                new CatletCapabilityConfig
                {
                    Name = "Cap1",
                    Details = ["detail"]
                }
            ]
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Capabilities =
            [
                new CatletCapabilityConfig
                {
                    Name = "Cap1",
                    Details = ["detail2"]
                }
            ]
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Capabilities.Should().SatisfyRespectively(
            capability => capability.Details.Should().BeEquivalentTo(["detail2"]));
    }

    [Fact]
    public void Drives_are_merged()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Drives =
            [
                new CatletDriveConfig
                {
                    Name = "sda",
                    Type = CatletDriveType.VHD,
                    Source = "gene:dbosoft/test/1.0:sda",
                },
                new CatletDriveConfig
                {
                    Name = "sdb",
                    Type = CatletDriveType.VHD,
                    Source = "gene:dbosoft/test/1.0:sdb",
                }
            ]
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Drives =
            [
                new CatletDriveConfig
                {
                    Name = "sda",
                    Store = "test-store",
                },
                new CatletDriveConfig
                {
                    Name = "sdb",
                    Type = CatletDriveType.PHD,
                    Store = "test-store",
                    Location = "test-location"
                }
            ]
        };

        var result = CatletBreeding.Breed(parent, child);

        CatletBreeding.Breed(parent, child).Should().BeRight().Which.Drives
            .Should().SatisfyRespectively(
                drive =>
                {
                    drive.Type.Should().Be(CatletDriveType.VHD);
                    drive.Store.Should().Be("test-store");
                    drive.Should().Be("gene:dbosoft/test/1.0:sda");
                },
                drive =>
                {
                    drive.Type.Should().Be(CatletDriveType.PHD);
                    drive.Store.Should().Be("test-store");
                    drive.Location.Should().Be("test-location");
                    drive.Source.Should().BeNull();
                });
    }

    [Fact]
    public void NetworkAdapters_are_merged()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            NetworkAdapters =
            [
                new CatletNetworkAdapterConfig()
                {
                    Name = "sda",
                    MacAddress = "addr1"
                }
            ]
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            NetworkAdapters =
            [
                new CatletNetworkAdapterConfig()
                {
                    Name = "sda",
                    MacAddress = "addr2"
                }
            ]
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.NetworkAdapters.Should().SatisfyRespectively(
            adapter => adapter.MacAddress.Should().Be("addr2"));
    }

    [Fact]
    public void Networks_are_merged()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Networks =
            [
                new CatletNetworkConfig()
                {
                    Name = "sda",
                    AdapterName = "eth2",
                    SubnetV4 = new CatletSubnetConfig
                    {
                        Name = "default",
                        IpPool = "other"
                    },
                    SubnetV6 = new CatletSubnetConfig
                    {
                        Name = "default",
                        IpPool = "default"
                    }
                }
            ]
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Networks =
            [
                new CatletNetworkConfig()
                {
                    Name = "sda",
                    AdapterName = "eth1",
                    SubnetV4 = new CatletSubnetConfig
                    {
                        Name = "none-default"
                    }
                }
            ]
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Networks.Should().SatisfyRespectively(
            network =>
            {
                network.AdapterName.Should().Be("eth1");
                network.SubnetV4.Should().NotBeNull();
                network.SubnetV4?.Name.Should().Be("none-default");
                network.SubnetV4?.IpPool.Should().BeNull();
                network.SubnetV6.Should().NotBeNull();
                network.SubnetV6?.Name.Should().Be("default");
                network.SubnetV6?.IpPool.Should().Be("default");
            });
    }

    [Fact]
    public void Memory_is_merged()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Memory = new CatletMemoryConfig
            {
                Startup = 2048,
                Minimum = 1024,
                Maximum = 9096
            }
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Memory = new CatletMemoryConfig
            {
                Startup = 2049,
                Minimum = 1025,
                Maximum = 9097
            }
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Memory.Should().NotBeNull();
        breedChild.Memory!.Startup.Should().Be(2049);
        breedChild.Memory!.Minimum.Should().Be(1025);
        breedChild.Memory!.Maximum.Should().Be(9097);
    }

    [Fact]
    public void Fodder_is_mixed()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = "cfg",
                    Type = "type1",
                    Content = "contenta",
                    FileName = "filenamea"
                }
            ]
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = "cfg",
                    Type = "type2",
                    Content = "contentb",
                    FileName = "filenameb"
                }
            ]
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().SatisfyRespectively(
            fodder =>
            {
                fodder.Type.Should().Be("type2");
                fodder.Content.Should().Be("contentb");
                fodder.FileName.Should().Be("filenameb");
            });
    }

    [Fact]
    public void Fodder_is_removed()
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = "cfg",
                    Type = "type1",
                    Content = "contenta",
                    FileName = "filenamea"
                }
            ]
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = "cfg",
                    Remove = true
                }
            ]
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().NotBeNull();
        breedChild.Fodder.Should().HaveCount(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("fodder")]
    public void Fodder_from_source_is_not_removed(string fodderName)
    {
        var parent = new CatletConfig
        {
            Name = "Parent",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = fodderName,
                    Source = "gene:somegene/utt/123:gene1",
                },
            ],
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = fodderName,
                    Source = "gene:somegene/utt/123:gene1",
                    Remove = true,
                },
            ],
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().SatisfyRespectively(
            fodder =>
            {
                fodder.Name.Should().Be(fodderName);
                fodder.Source.Should().Be("gene:somegene/utt/123:gene1");
                fodder.Remove.Should().BeTrue();
            });
    }

    [Fact]
    public void Fodder_with_and_without_source_is_mixed_separately()
    {
        var parent = new CatletConfig
        {
            Name = "parent",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = "fodder",
                    Content = "parent fodder content",
                },
                new FodderConfig()
                {
                    Source = "gene:somegene/utt/123:gene1",
                },
                new FodderConfig()
                {
                    Name = "fodder",
                    Source = "gene:somegene/utt/123:gene1",
                },
                new FodderConfig()
                {
                    Source = "gene:somegene/utt/123:gene2",
                },
            ],
        };

        var child = new CatletConfig
        {
            Name = "child",
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig()
                {
                    Name = "fodder",
                    Content = "child fodder content",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "testVariable",
                            Value = "child fodder value",
                        },
                    ],
                },
                new FodderConfig()
                {
                    Source = "gene:somegene/utt/123:gene1",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "testVariable",
                            Value = "gene 1 child fodder value",
                        },
                    ],
                },
                new FodderConfig()
                {
                    Name = "fodder",
                    Source = "gene:somegene/utt/123:gene1",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "testVariable",
                            Value = "gene 1 with name child fodder value",
                        },
                    ],
                },
                new FodderConfig()
                {
                    Source = "gene:somegene/utt/123:gene2",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "testVariable",
                            Value = "gene 2 child fodder value",
                        },
                    ],
                },
            ],
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().SatisfyRespectively(
            fodder =>
            {
                fodder.Name.Should().Be("fodder");
                // TODO Is this correct? Should be as the fodder was bred and hence the source is now the child.
                // fodder.Source.Should().Be("gene:dbosoft/test/1.0:catlet");
                fodder.Source.Should().BeNull();
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("testVariable");
                        variable.Value.Should().Be("child fodder value");
                    });
            },
            fodder =>
            {
                fodder.Name.Should().BeNull();
                fodder.Source.Should().Be("gene:somegene/utt/123:gene1");
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("testVariable");
                        variable.Value.Should().Be("gene 1 child fodder value");
                    });
            },
            fodder =>
            {
                fodder.Name.Should().Be("fodder");
                fodder.Source.Should().Be("gene:somegene/utt/123:gene1");
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("testVariable");
                        variable.Value.Should().Be("gene 1 with name child fodder value");
                    });
            },
            fodder =>
            {
                fodder.Name.Should().BeNull();
                fodder.Source.Should().Be("gene:somegene/utt/123:gene2");
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("testVariable");
                        variable.Value.Should().Be("gene 2 child fodder value");
                    });
            });
    }

    [Theory]
    [InlineData(MutationType.Merge, 2)]
    [InlineData(MutationType.Remove, 1)]
    [InlineData(MutationType.Overwrite, 2)]
    public void Mutates(MutationType type, int expectedCount)
    {
        var parent = new CatletConfig
        {
            Capabilities =
            [
                new CatletCapabilityConfig
                {
                    Name = "cap1",
                    Details = ["any"]
                },
                new CatletCapabilityConfig
                {
                    Name = "cap2"
                },
                new CatletCapabilityConfig
                {
                    Name = "cap3",
                    Mutation = MutationType.Remove
                }
            ]
        };

        var child = new CatletConfig
        {
            Parent = "dbosoft/test/1.0",
            Capabilities =
            [
                new CatletCapabilityConfig
                {
                    Name = "cap1",
                    Mutation = type,
                    Details = ["none"]
                },
                new CatletCapabilityConfig
                {
                    Name = "cap2",
                }
            ]
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Capabilities.Should().NotBeNull();
        breedChild.Capabilities.Should().HaveCount(expectedCount);

        if (type == MutationType.Overwrite)
        {
            breedChild.Capabilities![0].Details.Should().BeEquivalentTo(["none"]);
        }
    }


    [Fact]
    public void Variables_are_not_mutated_but_child_replaces_parent()
    {
        var parent = new CatletConfig
        {
            Variables =
            [
                new VariableConfig
                {
                    Name = "catletVariable",
                    Type = VariableType.Number,
                    Value = "4.2",
                    Required = true,
                    Secret = true,
                },
            ],
        };

        var child = new CatletConfig
        {
            Parent = "dbosoft/test/1.0",
            Variables =
            [
                new VariableConfig
                {
                    Name = "catletVariable",
                    Value = "string value",
                },
            ],
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Variables.Should().SatisfyRespectively(
            variable =>
            {
                variable.Name.Should().Be("catletVariable");
                variable.Type.Should().BeNull();
                variable.Value.Should().Be("string value");
                variable.Required.Should().BeNull();
                variable.Secret.Should().BeNull();
            });
    }

    [Fact]
    public void Variables_in_fodder_are_replaced_when_content_is_mutated()
    {
        var parent = new CatletConfig
        {
            Fodder =
            [
                new FodderConfig
                {
                    Name = "fodder",
                    Content = "parent fodder content",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "parentVariable",
                            Type = VariableType.Number,
                            Value = "4.2",
                            Required = true,
                            Secret = true,
                        },
                    ],
                },
            ],
        };

        var child = new CatletConfig
        {
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig
                {
                    Name = "fodder",
                    Content = "child fodder content",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "childVariable",
                            Value = "string value",
                        },
                    ],
                },
            ],
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().SatisfyRespectively(
            fodder =>
            {
                fodder.Content.Should().Be("child fodder content");
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("childVariable");
                        variable.Type.Should().BeNull();
                        variable.Value.Should().Be("string value");
                        variable.Required.Should().BeNull();
                        variable.Secret.Should().BeNull();
                    });
            });
    }

    public static readonly IEnumerable<string> fodderNames =
    [
        "test-fodder",
        "TEST-FODDER"
    ];

    public static readonly IEnumerable<string> genesets =
    [
        "somegene/utt/123",
        "SOMEGENE/UTT/123",
    ];

    [Theory, CombinatorialData]
    public void Variables_in_fodder_are_replaced_when_source_and_name_are_specified(
        [CombinatorialMemberData(nameof(fodderNames))] string parentFodderName,
        [CombinatorialMemberData(nameof(genesets))] string parentGeneset,
        [CombinatorialMemberData(nameof(fodderNames))] string childFodderName,
        [CombinatorialMemberData(nameof(genesets))] string childGeneset)
    {
        var parent = new CatletConfig
        {
            Fodder =
            [
                new FodderConfig
                {
                    Name = parentFodderName,
                    Source = $"gene:{parentGeneset}:gene1",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "parentVariable",
                            Type = VariableType.Number,
                            Value = "4.2",
                            Required = true,
                            Secret = true,
                        },
                    ],
                },
            ],
        };

        var child = new CatletConfig
        {
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig
                {
                    Name = childFodderName,
                    Source = $"gene:{childGeneset}:gene1",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "childVariable",
                            Value = "string value",
                        },
                    ],
                },
            ],
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().SatisfyRespectively(
            fodder =>
            {
                fodder.Name.Should().Be(childFodderName);
                fodder.Source.Should().Be($"gene:{childGeneset}:gene1");
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("childVariable");
                        variable.Type.Should().BeNull();
                        variable.Value.Should().Be("string value");
                        variable.Required.Should().BeNull();
                        variable.Secret.Should().BeNull();
                    });
            });
    }

    [Theory, CombinatorialData]
    public void Variables_in_fodder_are_replaced_when_source_is_specified(
    [CombinatorialMemberData(nameof(genesets))] string parentGeneset,
    [CombinatorialMemberData(nameof(genesets))] string childGeneset)
    {
        var parent = new CatletConfig
        {
            Fodder =
            [
                new FodderConfig
                {
                    Source = $"gene:{parentGeneset}:gene1",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "parentVariable",
                            Type = VariableType.Number,
                            Value = "4.2",
                            Required = true,
                            Secret = true,
                        },
                    ],
                },
            ],
        };

        var child = new CatletConfig
        {
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig
                {
                    Source = $"gene:{childGeneset}:gene1",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "childVariable",
                            Value = "string value",
                        },
                    ],
                },
            ],
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().SatisfyRespectively(
            fodder =>
            {
                fodder.Name.Should().BeNull();
                fodder.Source.Should().Be($"gene:{childGeneset}:gene1");
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("childVariable");
                        variable.Type.Should().BeNull();
                        variable.Value.Should().Be("string value");
                        variable.Required.Should().BeNull();
                        variable.Secret.Should().BeNull();
                    });
            });
    }

    [Fact]
    public void Variables_in_fodder_are_not_replaced_when_content_is_not_mutated()
    {
        var parent = new CatletConfig
        {
            Fodder =
            [
                new FodderConfig
                {
                    Name = "fodder",
                    Content = "parent fodder content",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "parentVariable",
                            Type = VariableType.Number,
                            Value = "4.2",
                            Required = true,
                            Secret = true,
                        },
                    ],
                },
            ],
        };

        var child = new CatletConfig
        {
            Parent = "dbosoft/test/1.0",
            Fodder =
            [
                new FodderConfig
                {
                    Name = "fodder",
                    Variables =
                    [
                        new VariableConfig
                        {
                            Name = "childVariable",
                            Value = "string value",
                        },
                    ],
                },
            ],
        };

        var breedChild = CatletBreeding.Breed(parent, child)
            .Should().BeRight().Subject;

        breedChild.Fodder.Should().SatisfyRespectively(
            fodder =>
            {
                fodder.Content.Should().Be("parent fodder content");
                fodder.Variables.Should().SatisfyRespectively(
                    variable =>
                    {
                        variable.Name.Should().Be("parentVariable");
                        variable.Type.Should().Be(VariableType.Number);
                        variable.Value.Should().Be("4.2");
                        variable.Required.Should().BeTrue();
                        variable.Secret.Should().BeTrue();
                    });
            });
    }

    // TODO test duplicate entries fodder, drives, etc.
}
