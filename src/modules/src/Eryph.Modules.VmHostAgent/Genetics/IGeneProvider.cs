﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Eryph.ConfigModel;
using Eryph.Core.Genetics;
using Eryph.Messages.Resources.Genes.Commands;
using LanguageExt;
using LanguageExt.Common;

namespace Eryph.Modules.VmHostAgent.Genetics;

public interface IGeneProvider
{
    EitherAsync<Error, PrepareGeneResponse> ProvideGene(
        GeneType geneType,
        GeneIdentifier geneIdentifier,
        Func<string, int, Task<Unit>> reportProgress,
        CancellationToken cancel);

    EitherAsync<Error, GeneSetIdentifier> ResolveGeneSet(
        GeneSetIdentifier genesetIdentifier,
        Func<string, int, Task<Unit>> reportProgress,
        CancellationToken cancellationToken);

    EitherAsync<Error, Option<string>> GetGeneSetParent(
        GeneSetIdentifier genesetIdentifier,
        Func<string, int, Task<Unit>> reportProgress,
        CancellationToken cancellationToken);
}