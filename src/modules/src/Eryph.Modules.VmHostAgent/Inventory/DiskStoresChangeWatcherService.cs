﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Eryph.Core;
using Eryph.Core.VmAgent;
using LanguageExt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

using static LanguageExt.Prelude;
using static LanguageExt.Seq;

namespace Eryph.Modules.VmHostAgent.Inventory;

public sealed class DiskStoresChangeWatcherService(
    IBus bus,
    ILogger logger,
    IHostSettingsProvider hostSettingsProvider,
    IVmHostAgentConfigurationManager vmHostAgentConfigManager,
    InventoryConfig inventoryConfig)
    : IHostedService, IDisposable
{
    private IDisposable? _subscription;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _stopping;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Restart();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Do not pass the cancellationToken as we must always wait
        // for the semaphore. Another thread might be restarting the
        // watchers at this moment. We must wait for that thread to
        // complete so we can stop the watchers correctly.
#pragma warning disable CA2016
        await _semaphore.WaitAsync();
#pragma warning restore CA2016
        try
        {
            _stopping = true;
            _subscription?.Dispose();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task Restart()
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_stopping)
                return;

            logger.LogInformation("Starting watcher for disk stores with latest settings...");

            _subscription?.Dispose();

            var vmHostAgentConfig = await GetConfig();
            var paths = append(
                vmHostAgentConfig.Environments.ToSeq()
                    .Bind(e => e.Datastores.ToSeq())
                    .Filter(ds => !ds.WatchFileSystem)
                    .Map(ds => ds.Path),
                vmHostAgentConfig.Environments.ToSeq()
                    .Filter(e => !e.Defaults.WatchFileSystem)
                    .Map(e => e.Defaults.Volumes),
                vmHostAgentConfig.Datastores.ToSeq()
                    .Filter(ds => !ds.WatchFileSystem)
                    .Map(ds => ds.Path),
                Seq1(vmHostAgentConfig.Defaults)
                    .Filter(d => d.WatchFileSystem)
                    .Map(d => d.Volumes));

            _subscription = ObserveStores(paths).Subscribe();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<VmHostAgentConfiguration> GetConfig()
    {
        var result = await hostSettingsProvider.GetHostSettings()
            .Bind(vmHostAgentConfigManager.GetCurrentConfiguration)
            .ToAff(identity)
            .Run();

        return result.ThrowIfFail();
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        _semaphore.Dispose();
    }

    /// <summary>
    /// Creates an <see cref="IObservable{T}"/> which monitors the given
    /// <paramref name="paths"/>.
    /// </summary>
    /// <remarks>
    /// This method internally uses multiple <see cref="FileSystemWatcher"/>s
    /// to monitor the <paramref name="paths"/>. For simplicity, all their
    /// events are folded into a single event stream. The event stream is throttled
    /// to avoid triggering too many inventory actions. Every event, which emerges
    /// at the end, triggers a full inventory of all disk stores by raising a
    /// <see cref="DiskStoreChangedEvent"/> via the local Rebus.
    /// </remarks>
    private IObservable<System.Reactive.Unit> ObserveStores(Seq<string> paths) =>
        paths.ToObservable()
            .SelectMany(ObservePath)
            .Throttle(inventoryConfig.DiskEventDelay)
            .Select(_ => Observable.FromAsync(() => bus.SendLocal(new DiskStoresChangedEvent())))
            .Concat();

    private IObservable<FileSystemEventArgs> ObservePath(string path) =>
        Observable.Defer(() =>
        {
            if (Directory.Exists(path))
                return Observable.Return(path);

            logger.LogWarning("The store path '{Path}' does not exist and will not be monitored.", path);
            return Observable.Return<string>(null);
        })
        .Where(p => p is not null)
        .SelectMany(
            Observe(() => new FileSystemWatcher(path)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.DirectoryName,
            }).Merge(Observe(() => new FileSystemWatcher(path)
            {
                EnableRaisingEvents = true,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.vhdx",
            })))
        .Where(fsw => fsw is not null)
        .SelectMany(fsw => Observable.Merge(
                Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => fsw.Created += h, h => fsw.Created -= h),
                Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    h => fsw.Deleted += h, h => fsw.Deleted -= h),
                Observable.FromEventPattern<RenamedEventHandler, FileSystemEventArgs>(
                    h => fsw.Renamed += h, h => fsw.Renamed -= h))
            .Select(ep => ep.EventArgs)
            .Finally(fsw.Dispose));

    /// <summary>
    /// Tries to create the <see cref="FileSystemWatcher"/> and returns <see langword="null"/>
    /// when the <see cref="FileSystemWatcher"/> cannot be created.
    /// </summary>
    /// <remarks>
    /// The constructor of <see cref="FileSystemWatcher"/> throws when the path is not accessible
    /// or does not exist.
    /// </remarks>
    private IObservable<FileSystemWatcher> Observe(Func<FileSystemWatcher> factory)
    {
        try
        {
            var watcher = factory();
            return Observable.Return(watcher);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create file system watcher. The corresponding path will not be monitored.");
            return Observable.Return<FileSystemWatcher>(null);
        }
    }
}
