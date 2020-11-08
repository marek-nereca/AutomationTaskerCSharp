using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Q42.HueApi.Extensions;
using Serilog;

namespace Tasker
{
    public class ActionScheduler
    {
        private readonly ILogger _log;

        private readonly ConcurrentDictionary<string, CancellationTokenSource> _actionIds =
            new ConcurrentDictionary<string, CancellationTokenSource>();

        public ActionScheduler(ILogger logger)
        {
            _log = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task RegisterAction(string actionId, Action action, TimeSpan timeSpan,
            CancellationToken parentCancellationToken)
        {
            if (_actionIds.TryRemove(actionId, out var previousCancellation))
            {
                previousCancellation.Cancel();
                _log.Information("Previously scheduled action {actionId} cancelled", actionId);
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var linkedTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationTokenSource.Token, parentCancellationToken);
            var linkedToken = linkedTokenSource.Token;

            _actionIds.GetOrAdd(actionId, cancellationTokenSource);
            _log.Information("Scheduling action {actionId} for UTC {time}", actionId, DateTime.UtcNow.Add(timeSpan).ToString("s"));

            var task = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(timeSpan, linkedToken);
                }
                catch (TaskCanceledException)
                {
                }

                _actionIds.TryRemove(actionId, out _);
                if (!linkedToken.IsCancellationRequested)
                {
                    try
                    {
                        _log.Information("Invoking scheduled action {actionId}", actionId);
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        _log.Error(e, "Scheduled action failed with id {actionId}", actionId);
                    }
                }
            }, parentCancellationToken);
            return task;
        }
    }
}