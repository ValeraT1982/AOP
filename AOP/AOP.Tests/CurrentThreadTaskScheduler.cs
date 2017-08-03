using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AOP.Tests
{
    public class CurrentThreadTaskScheduler: TaskScheduler
    {
        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return Enumerable.Empty<Task>();
        }

        protected override void QueueTask(Task task)
        {
            TryExecuteTask(task);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            TryExecuteTask(task);
            return true;
        }
    }
}
