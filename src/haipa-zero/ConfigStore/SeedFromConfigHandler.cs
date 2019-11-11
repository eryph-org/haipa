﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Haipa.Modules;
using LanguageExt;

namespace Haipa.Runtime.Zero.ConfigStore
{
    internal class SeedFromConfigHandler<TModule> : IModuleHandler where TModule : IModule
    {
        private readonly IEnumerable<IConfigSeeder<TModule>> _seeders;

        public SeedFromConfigHandler(IEnumerable<IConfigSeeder<TModule>> seeders)
        {
            _seeders = seeders;
        }

        public Task Execute(CancellationToken stoppingToken)
        {
            return _seeders.AsTask().MapT(s => s.Execute(stoppingToken));
        }
    }
}