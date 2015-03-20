using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Nito.AsyncEx
{
    /// <summary>
    /// A thread that executes actions within an <see cref="AsyncContext"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(DebugView))]
    public sealed class AsyncContextThread : IDisposable
    {
        /// <summary>
        /// The child thread.
        /// </summary>
        private readonly Task _thread;

        /// <summary>
        /// The asynchronous context executed by the child thread.
        /// </summary>
        private readonly AsyncContext _context;

        /// <summary>
        /// A flag used to ensure we only call <see cref="AsyncContext.OperationCompleted"/> once during complex join/dispose operations.
        /// </summary>
        private int _stoppingFlag;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncContextThread"/> class, creating a child thread waiting for commands.
        /// </summary>
        public AsyncContextThread()
        {
            _context = new AsyncContext();
            _context.SynchronizationContext.OperationStarted();
            _thread = Task.Factory.StartNew(Execute, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        /// <summary>
        /// Gets the <see cref="AsyncContext"/> executed by this thread.
        /// </summary>
        public AsyncContext Context
        {
            get { return _context; }
        }

        private void Execute()
        {
            using (_context)
            {
                _context.Execute();
            }
        }

        /// <summary>
        /// Permits the thread to exit, if we have not already done so.
        /// </summary>
        private void AllowThreadToExit()
        {
            if (Interlocked.CompareExchange(ref _stoppingFlag, 1, 0) == 0)
            {
                _context.SynchronizationContext.OperationCompleted();
            }
        }

        /// <summary>
        /// Requests the thread to exit and returns a task representing the exit of the thread. The thread will exit when all outstanding asynchronous operations complete.
        /// </summary>
        public Task JoinAsync()
        {
            AllowThreadToExit();
            return _thread;
        }

        /// <summary>
        /// Requests the thread to exit.
        /// </summary>
        public void Dispose()
        {
            AllowThreadToExit();
        }

        /// <summary>
        /// Gets the <see cref="TaskFactory"/> for this thread, which can be used to schedule work to this thread.
        /// </summary>
        public TaskFactory Factory
        {
            get { return _context.Factory; }
        }

        [DebuggerNonUserCode]
        internal sealed class DebugView
        {
            private readonly AsyncContextThread _thread;

            public DebugView(AsyncContextThread thread)
            {
                _thread = thread;
            }

            public AsyncContext Context
            {
                get { return _thread.Context; }
            }

            public object Thread
            {
                get { return _thread._thread; }
            }
        }
    }
}
