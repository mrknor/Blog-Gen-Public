using System.Collections.Concurrent;
using System.Threading.Channels;

namespace GenFarm.Services
{
    public class BackgroundTaskQueue
    {
        private readonly Channel<Func<CancellationToken, ValueTask>> _queue;
        private readonly ConcurrentDictionary<Guid, string> _taskStatuses;

        public BackgroundTaskQueue(int capacity)
        {
            _queue = Channel.CreateBounded<Func<CancellationToken, ValueTask>>(capacity);
            _taskStatuses = new ConcurrentDictionary<Guid, string>();
        }

        public ValueTask QueueBackgroundWorkItemAsync(Func<CancellationToken, ValueTask> workItem, Guid taskId)
        {
            if (workItem == null) throw new ArgumentNullException(nameof(workItem));

            // Initialize task status as "Queued"
            _taskStatuses[taskId] = "Queued";

            return _queue.Writer.WriteAsync(async token =>
            {
                _taskStatuses[taskId] = "In Progress";
                try
                {
                    await workItem(token);
                    _taskStatuses[taskId] = "Completed";
                }
                catch
                {
                    _taskStatuses[taskId] = "Failed";
                }
            });
        }

        public async ValueTask<Func<CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
        {
            var workItem = await _queue.Reader.ReadAsync(cancellationToken);
            return workItem;
        }

        public string GetTaskStatus(Guid taskId)
        {
            _taskStatuses.TryGetValue(taskId, out var status);
            return status ?? "Unknown";
        }
    }

}
