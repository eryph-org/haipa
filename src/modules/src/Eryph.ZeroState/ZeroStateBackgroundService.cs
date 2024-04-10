﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimpleInjector;
using SimpleInjector.Lifestyles;

namespace Eryph.ZeroState;

internal class ZeroStateBackgroundService<TChange> : BackgroundService
{
    private readonly Container _container;
    private readonly ILogger<ZeroStateBackgroundService<TChange>> _logger;
    private readonly IZeroStateQueue<TChange> _queue;

    public ZeroStateBackgroundService(
        Container container,
        ILogger<ZeroStateBackgroundService<TChange>> logger,
        IZeroStateQueue<TChange> queue)
    {
        _container = container;
        _logger = logger;
        _queue = queue;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var queueItem = await _queue.DequeueAsync(stoppingToken);
                await HandleChangeAsync(queueItem);
            }
            catch (OperationCanceledException)
            {
                // The exception will occur when the host is stopped.
                // We swallow it and flush the remaining queue.
            }
        }

        await FlushQueue();
    }

    private async Task FlushQueue()
    {
        _logger.LogInformation("Going to flush {Count} remaining queue item(s).",
            _queue.GetCount());
        while (_queue.GetCount() > 0)
        {
            var queueItem = await _queue.DequeueAsync();
            await HandleChangeAsync(queueItem);
        }
    }

    private async Task HandleChangeAsync(ZeroStateQueueItem<TChange> queueItem)
    {
        _logger.LogInformation("Processing changes of transaction {TransactionId}", queueItem.TransactionId);

        try
        {
            await using var scope = AsyncScopedLifestyle.BeginScope(_container);
            var handler = scope.GetInstance<IZeroStateChangeHandler<TChange>>();
            await handler.HandleChangeAsync(queueItem.Changes);
        }
        catch (Exception ex)
        {
            // We need to catch all exceptions here. Otherwise, the background
            // service will stop when an exception occurs. We always want to write
            // as many of the changes as possible to the filesystem.
            _logger.LogError(ex, "Failed to process changes of transaction {TransactionId}", queueItem.TransactionId);
        }
    }
}
