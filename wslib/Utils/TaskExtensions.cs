using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Utils
{
    public static class TaskExtensions
    {
        public static async Task AbortOnTimeout(this Task responseTask, Action abortAction, TimeSpan timeout, CancellationToken token)
        {
            using (var cancelSource = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                Task timeoutTask = Task.Delay(timeout, cancelSource.Token);

                if (responseTask == await Task.WhenAny(responseTask, timeoutTask).ConfigureAwait(false))
                {
                    cancelSource.Cancel(); // cancel the delay task                                                                           
                    return;
                }
            }

            // timeout or cancellation token has fired                                                                                        
            abortAction();
            token.ThrowIfCancellationRequested();
            throw new TimeoutException();
        }

        public static async Task<TResult> AbortOnTimeout<TResult>(this Task<TResult> responseTask, Action abortAction, TimeSpan timeout, CancellationToken token)
        {
            await AbortOnTimeout((Task)responseTask, abortAction, timeout, token).ConfigureAwait(false);
            return await responseTask.ConfigureAwait(false);
        }

        public static Task<TResult> WithTimeout<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            return AbortOnTimeout(task, () => { }, timeout, CancellationToken.None);
        }

        public static Task WithTimeout(this Task task, TimeSpan timeout)
        {
            return AbortOnTimeout(task, () => { }, timeout, CancellationToken.None);
        }
    }
}