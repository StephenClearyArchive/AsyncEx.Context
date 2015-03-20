using System;
using System.Threading.Tasks;
using Nito.AsyncEx;
using System.Linq;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class AsyncContextThreadUnitTests
    {
        [TestMethod]
        public async Task AsyncContextThread_IsAnIndependentThread()
        {
            var testThread = Thread.CurrentThread.ManagedThreadId;
            var thread = new AsyncContextThread();
            var contextThread = await thread.Factory.Run(() => Thread.CurrentThread.ManagedThreadId);
            Assert.AreNotEqual(testThread, contextThread);
            await thread.JoinAsync();
        }

        [TestMethod]
        public async Task AsyncDelegate_ResumesOnSameThread()
        {
            var thread = new AsyncContextThread();
            int contextThread = -1, resumeThread = -1;
            await thread.Factory.Run(async () =>
            {
                contextThread = Thread.CurrentThread.ManagedThreadId;
                await Task.Yield();
                resumeThread = Thread.CurrentThread.ManagedThreadId;
            });
            Assert.AreEqual(contextThread, resumeThread);
            await thread.JoinAsync();
        }

        [TestMethod]
        public async Task Join_StopsTask()
        {
            var context = new AsyncContextThread();
            var thread = await context.Factory.Run(() => Thread.CurrentThread);
            await context.JoinAsync();
        }

        [TestMethod]
        public async Task Context_IsCorrectAsyncContext()
        {
            using (var thread = new AsyncContextThread())
            {
                var observedContext = await thread.Factory.Run(() => AsyncContext.Current);
                Assert.AreSame(observedContext, thread.Context);
            }
        }
    }
}
