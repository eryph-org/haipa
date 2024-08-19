﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Eryph.ConfigModel;
using Eryph.GenePool.Model;
using LanguageExt;
using LanguageExt.Common;

namespace Eryph.Modules.VmHostAgent.Genetics;

internal interface ILocalGenePool: IGenePool
{
    EitherAsync<Error, Unit> MergeGenes(GeneInfo geneInfo, GeneSetInfo geneSetInfo,
        Func<string, int, Task<Unit>> reportProgress, CancellationToken cancel);

    EitherAsync<Error, GeneSetInfo> CacheGeneSet(string path, GeneSetInfo geneSetInfo, CancellationToken cancel);

    EitherAsync<Error, GeneInfo> CacheGene(GeneInfo geneInfo, GeneSetInfo geneSetInfo, CancellationToken cancel);
}