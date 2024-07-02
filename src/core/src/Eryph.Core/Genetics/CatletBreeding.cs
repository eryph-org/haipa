﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Eryph.ConfigModel;
using Eryph.ConfigModel.Catlets;
using Eryph.ConfigModel.Variables;
using JetBrains.Annotations;
using LanguageExt;
using LanguageExt.Common;

using static LanguageExt.Prelude;

namespace Eryph.Core.Genetics;

public static class CatletBreeding
{
    /// <summary>
    /// Breeds the <paramref name="parentConfig"/> with the <paramref name="childConfig"/>.
    /// The <see cref="CatletConfig.Parent"/> of the <paramref name="childConfig"/>
    /// must be the properly resolved identifier for the <paramref name="parentConfig"/>.
    /// </summary>
    public static Either<Error, CatletConfig> Breed(
        CatletConfig parentConfig,
        CatletConfig childConfig) =>
        from parentId in GeneSetIdentifier.NewEither(childConfig.Parent)
            .MapLeft(e => Error.New("The child must contain a valid parent during breeding.", e))
        from cpu in BreedCpu(parentConfig.Cpu, childConfig.Cpu)
        from memory in BreedMemory(parentConfig.Memory, childConfig.Memory)
        from drives in BreedDrives(Seq(parentConfig.Drives), Seq(childConfig.Drives), parentId)
        from networkAdapters in BreedNetworkAdapters(Seq(parentConfig.NetworkAdapters), Seq(childConfig.NetworkAdapters))
        from networks in BreedNetworks(Seq(parentConfig.Networks), Seq(childConfig.Networks))
        from variables in BreedVariables(Seq(parentConfig.Variables), Seq(childConfig.Variables))
        from fodder in BreedFodder(Seq(parentConfig.Fodder), Seq(childConfig.Fodder), parentId)
        from capabilities in BreedCapabilities(Seq(parentConfig.Capabilities), Seq(childConfig.Capabilities))
        select new CatletConfig()
        {
            // Basic catlet configuration like name and placement information
            // is not inherited from parent
            Name = childConfig.Name,
            Parent = childConfig.Parent,
            Version = childConfig.Version,
            Project = childConfig.Project,
            Location = childConfig.Location,
            Environment = childConfig.Environment,
            Store = childConfig.Store,
            Hostname = childConfig.Hostname,

            // Bred configuration
            Capabilities = capabilities.ToArray(),
            Cpu = cpu.IfNoneUnsafe((CatletCpuConfig)null),
            Drives = drives.ToArray(),
            Fodder = fodder.ToArray(),
            Memory = memory.IfNoneUnsafe((CatletMemoryConfig)null),
            Networks = networks.ToArray(),
            NetworkAdapters = networkAdapters.ToArray(),
            Variables = variables.ToArray(),
        };

    public static Either<Error, Option<CatletCpuConfig>> BreedCpu(
        Option<CatletCpuConfig> parentConfig,
        Option<CatletCpuConfig> childConfig) =>
        BreedOptional(parentConfig, childConfig,
            (parent, child) => new CatletCpuConfig()
            {
                Count = child.Count ?? parent.Count
            });

    public static Either<Error, Option<CatletMemoryConfig>> BreedMemory(
        Option<CatletMemoryConfig> parentConfig,
        Option<CatletMemoryConfig> childConfig) =>
        BreedOptional(parentConfig, childConfig,
            (parent, child) => new CatletMemoryConfig()
            {
                Minimum = child.Minimum ?? parent.Minimum,
                Maximum = child.Maximum ?? parent.Maximum,
                Startup = child.Startup ?? parent.Startup,
            });

    public static Either<Error, Seq<CatletDriveConfig>> BreedDrives(
        Seq<CatletDriveConfig> parentDrives,
        Seq<CatletDriveConfig> childDrives,
        GeneSetIdentifier parentId) =>
        BreedMutateable(parentDrives, childDrives,
            CatletDriveName.NewEither,
            (parent, child) => new CatletDriveConfig()
            {
                Name = child.Name,
                Mutation = child.Mutation,

                Type = child.Type ?? parent.Type,
                Location = child.Location ?? parent.Location,
                Size = (child.Size ?? 0) != 0 ? child.Size : parent.Size,
                Store = notEmpty(child.Store) ? child.Store : parent.Store,
                Source = (Optional(child.Source).Filter(notEmpty) | Optional(parent.Source).Filter(notEmpty))
                    .Filter(_ =>
                        (Optional(child.Type) | Optional(parent.Type) | CatletDriveType.VHD) != CatletDriveType.VHD)
                    .IfNoneUnsafe((string)null)
            },
            child =>
                from adoptedSource in Optional(child.Source).Filter(notEmpty).Match(
                    Some: s => s,
                    None: () =>
                        from geneName in GeneName.NewEither(child.Name)
                        select new GeneIdentifier(parentId, geneName).Value)
                select child.CloneWith(c => c.Source = adoptedSource));

    public static Either<Error, Seq<CatletNetworkAdapterConfig>> BreedNetworkAdapters(
        Seq<CatletNetworkAdapterConfig> parentDrives,
        Seq<CatletNetworkAdapterConfig> childDrives) =>
        BreedMutateable(parentDrives, childDrives,
            CatletNetworkAdapterName.NewEither,
            (parent, child) => new CatletNetworkAdapterConfig()
            {
                Name = child.Name,
                Mutation = child.Mutation,

                MacAddress = child.MacAddress ?? parent.MacAddress,
            },
            child => child.Clone());

    public static Either<Error, Seq<CatletNetworkConfig>> BreedNetworks(
        Seq<CatletNetworkConfig> parentDrives,
        Seq<CatletNetworkConfig> childDrives) =>
        BreedMutateable(parentDrives, childDrives,
            EryphNetworkName.NewEither,
            (parent, child) => new CatletNetworkConfig()
            {
                Name = child.Name,
                Mutation = child.Mutation,

                AdapterName = child.AdapterName ?? parent.AdapterName,
                SubnetV4 = child.SubnetV4?.Clone() ?? parent.SubnetV4?.Clone(),
                SubnetV6 = child.SubnetV6?.Clone() ?? parent.SubnetV6?.Clone(),
            },
            child => child.Clone());

    public static Either<Error, Seq<VariableConfig>> BreedVariables(
        Seq<VariableConfig> parentDrives,
        Seq<VariableConfig> childDrives) =>
        from parentsWithNames in parentDrives
            .Map(c => from validName in VariableName.NewEither(c.Name)
                select new ConfigWithName<VariableName, VariableConfig>(validName, c))
            .Sequence()
        from childrenWithNames in childDrives
            .Map(c => from validName in VariableName.NewEither(c.Name)
                select new ConfigWithName<VariableName, VariableConfig>(validName, c))
            .Sequence()
        let childrenMap = childrenWithNames.Map(v => (v.Name, v.Config)).ToHashMap()
        let merged = parentsWithNames
            .Map(p => childrenMap.Find(p.Name).Match(
                // Merging a variable config is potentially problematic, e.g. the merge could
                // remove the secret flag without the user realizing the variable's value is
                // sensitive. Hence, a child variable always completely replaces the parent variable.
                Some: c => new ConfigWithName<VariableName, VariableConfig>(p.Name, c.Clone()),
                None: () => new ConfigWithName<VariableName, VariableConfig>(p.Name, p.Config.Clone())))
        let mergedMap = merged.Map(v => (v.Name, v.Config)).ToHashMap()
        let additional = childrenWithNames
            .Filter(c => mergedMap.Find(c.Name).IsNone)
            .Map(c => c with { Config = c.Config.Clone() })
        select merged.Concat(additional).Map(c => c.Config);

    public static Either<Error, Seq<FodderConfig>> BreedFodder(
        Seq<FodderConfig> parentDrives,
        Seq<FodderConfig> childDrives, 
        GeneSetIdentifier parentId) =>
        // TODO check fodder configs are unique
        // TODO check genesets are unique (not multiple tags per geneset)
        from parentsWithKeys in parentDrives
            .Map(parent => parent.CloneWith(c =>
            {
                c.Source = Optional(c.Source).Filter(notEmpty)
                    .IfNone(() => new GeneIdentifier(parentId, GeneName.New("catlet")).Value);
            }))
            .Map(FodderWithKey.Create)
            .Sequence()
        from childrenWithKeys in childDrives
            .Map(child => child.Clone())
            .Map(FodderWithKey.Create)
            .Sequence()
        let childrenMap = childrenWithKeys.Map(v => (v.Key, v.Config)).ToHashMap()
        from merged in parentsWithKeys
            .Filter(p => IsFodderGene(p.Key) || p.Config.Remove != true)
            .Filter(p => IsFodderGene(p.Key) || childrenMap.Find(p.Key).Filter(c => c.Remove == true).IsNone)
            .Map(p => childrenMap.Find(p.Key).Match(
                    Some: c => MergeFodder(p.Config, c),
                    None: () => p.Config.Clone())
                .Map(cfg => new FodderWithKey(p.Key, cfg)))
            .Sequence()
        let mergedMap = merged.Map(v => (v.Key, v.Config)).ToHashMap()
        let additional = childrenWithKeys
            .Filter(c => IsFodderGene(c.Key) || c.Config.Remove != true)
            .Filter(c => mergedMap.Find(c.Key).IsNone)
        select merged.Concat(additional).Map(c => c.Config);

    private static bool IsFodderGene(FodderKey fodderKey) =>
        fodderKey.Source.Filter(s => s.GeneName != GeneName.New("catlet")).IsSome;

    private static Either<Error, FodderConfig> MergeFodder(FodderConfig parent, FodderConfig child) =>
        new FodderConfig()
        {
            // Name and source should be the same for parent and child.
            // Otherwise, we would not merge
            Name = child.Name,
            Source = child.Source,

            Content = child.Content ?? parent.Content,
            FileName = child.FileName ?? parent.FileName,
            Remove = child.Remove ?? parent.Remove,
            Secret = child.Secret ?? parent.Secret,
            Type = child.Type ?? parent.Type,

            // A parameterized fodder content is only useful with its corresponding
            // variables. Hence, we take the variables from the fodder config which
            // provides the content or the source.
            Variables = child.Content is not null || child.Source is not null
                ? child.Variables?.Select(x => x.Clone()).ToArray()
                : parent.Variables?.Select(x => x.Clone()).ToArray(),
        };

    /// <summary>
    /// This record is used to identify a fodder (reference) for
    /// deduplication. It makes use of <see cref="FodderName"/>
    /// and <see cref="GeneIdentifier"/> which handle the respective
    /// normalization.
    /// </summary>
    private sealed record FodderKey
    {
        private FodderKey() { }
        
        public Option<FodderName> Name { get; private init; }

        public Option<GeneIdentifier> Source { get; private init; }

        public static Either<Error, FodderKey> Create(Option<string> name, Option<string> source) =>
            from validName in name.Filter(notEmpty)
                .Map(FodderName.NewEither)
                .Sequence()
            from validSource in source.Filter(notEmpty)
                .Map(GeneIdentifier.NewEither)
                .Sequence()
            from _ in guardnot(validName.IsNone && validSource.IsNone,
                Error.New("Found invalid fodder which neither name nor source."))
            let fodderKey = new FodderKey
            {
                Name = validName,
                Source = validSource,
            }
            // The breeding injects informational sources for fodder taken from
            // the parent (which uses the gene name 'catlet'). These sources must
            // be ignored during deduplication.
            from __ in guardnot(!IsFodderGene(fodderKey) && validName.IsNone,
                Error.New($"Found catlet fodder without name ({validSource.Map(s => s.Value).IfNone("")})"))
            select IsFodderGene(fodderKey) ? fodderKey : new FodderKey { Name = fodderKey.Name };
    }

    private sealed record FodderWithKey(FodderKey Key, FodderConfig Config)
    {
        public static Either<Error, FodderWithKey> Create(FodderConfig config) =>
            from fodderKey in FodderKey.Create(config.Name, config.Source)
            select new FodderWithKey(fodderKey, config);
    };

    public static Either<Error, Seq<CatletCapabilityConfig>> BreedCapabilities(
        Seq<CatletCapabilityConfig> parentDrives,
        Seq<CatletCapabilityConfig> childDrives) =>
        BreedMutateable(parentDrives, childDrives,
            CatletCapabilityName.NewEither,
            (parent, child) => new CatletCapabilityConfig()
            {
                Name = child.Name,
                Details = child.Details?.ToArray() ?? parent.Details?.ToArray(),
            },
            child => child.Clone());

    private static Either<Error, Option<TConfig>> BreedOptional<TConfig>(
        Option<TConfig> parentConfig,
        Option<TConfig> childConfig,
        Func<TConfig,TConfig,TConfig> breed)
        where TConfig : ICloneableConfig<TConfig> =>
        parentConfig.Match(
            Some: parent => childConfig.Match(
                Some: child => breed(parent, child),
                None: parent.Clone()),
            None: childConfig.Map(c => c.Clone()));

    private static Either<Error, Seq<TConfig>> BreedMutateable<TConfig, TName>(
        Seq<TConfig> parentConfigs,
        Seq<TConfig> childConfigs,
        Func<string, Either<Error, TName>> parseName,
        Func<TConfig, TConfig, Either<Error, TConfig>> merge,
        Func<TConfig, Either<Error, TConfig>> adopt)
        where TConfig : IMutateableConfig<TConfig>
        where TName : EryphName<TName> =>
        from parentsWithName in parentConfigs
            .Map(c => from name in parseName(c.Name)
                      select new ConfigWithName<TName, TConfig>(name, c))
            .Sequence()
        from _ in EnsureDistinct(parentsWithName.Map(p => p.Name))
            .MapLeft(duplicate =>  Error.New(
                $"The {nameof(TName)} '{duplicate}' is not unique in the parent config."))
        from childrenWithName in childConfigs
            .Map(c => from name in parseName(c.Name)
                      from adoptedConfig in adopt(c)
                      select new ConfigWithName<TName, TConfig>(name, adoptedConfig))
            .Sequence()
        let childrenMap = childrenWithName.Map(v => (v.Name, v.Config)).ToHashMap()
        from __ in EnsureDistinct(childrenWithName.Map(p => p.Name))
            .MapLeft(duplicate => Error.New(
                $"The {nameof(TName)} '{duplicate}' is not unique in the parent config."))
        from merged in parentsWithName
            .Filter(p => p.Config.Mutation != MutationType.Remove)
            .Filter(p => childrenMap.Find(p.Name).Filter(c => c.Mutation is MutationType.Remove).IsNone)
            .Map(p => childrenMap.Find(p.Name).Match(
                    Some: c => Optional(c.Mutation).Filter(m => m == MutationType.Overwrite).Match(
                            Some: _ => c,
                            None: () => merge(p.Config, c)),
                    None: () => p.Config.Clone())
                .Map(c => new ConfigWithName<TName, TConfig>(p.Name, c)))
            .Sequence()
        let mergedMap = merged.Map(v => (v.Name, v.Config)).ToHashMap()
        let additional = childrenWithName
            .Filter(c => c.Config.Mutation != MutationType.Remove)
            .Filter(c => mergedMap.Find(c.Name).IsNone)
        select merged.Concat(additional).Map(c => c.Config);


    private sealed record ConfigWithName<TName, TConfig>(TName Name, TConfig Config)
        where TName : EryphName<TName>;

    private static Either<TKey, Unit> EnsureDistinct<TKey>(
        Seq<TKey> keys)
        where TKey : IEquatable<TKey> =>
        keys.ToLookup(v => v).Find(g => g.Count() > 1).Match(
                Some: g => Left<TKey,Unit>(g.Key),
                None: () => Right<TKey, Unit>(unit));
}
