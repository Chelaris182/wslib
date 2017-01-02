using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Utils
{
    public static class TaskExtensions
    {
        public static async Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            await WithTimeout((Task)task, timeout).ConfigureAwait(false);
            return task.GetAwaiter().GetResult();
        }

        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            using (var cancelSource = new CancellationTokenSource())
            {
                Task timeoutTask = Task.Delay(timeout, cancelSource.Token);
                if (task == await Task.WhenAny(task, timeoutTask).ConfigureAwait(false))
                {
                    cancelSource.Cancel();
                    task.GetAwaiter().GetResult();
                    return;
                }
            }

            throw new TimeoutException();
        }
    }
}