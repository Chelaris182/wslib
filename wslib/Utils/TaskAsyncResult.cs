using System;
using System.Threading;
using System.Threading.Tasks;

namespace wslib.Utils
{
    public class TaskAsyncResult : IAsyncResult
    {
        public TaskAsyncResult(Task task, object state)
        {
            Task = task;
            AsyncState = state;
        }

        public Task Task { get; }

        public object AsyncState { get; }

        public WaitHandle AsyncWaitHandle => ((IAsyncResult)Task).AsyncWaitHandle;

        public bool CompletedSynchronously => ((IAsyncResult)Task).CompletedSynchronously;

        public bool IsCompleted => Task.IsCompleted;
    }

    public class TaskAsyncResult<T> : TaskAsyncResult
    {
        public TaskAsyncResult(Task<T> task, object state)
            : base(task, state)
        {
        }

        public T Result => ((Task<T>)Task).Result;
    }
}