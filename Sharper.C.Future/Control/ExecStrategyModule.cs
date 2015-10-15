using System;
using System.Threading;
using System.Threading.Tasks;
using Sharper.C.Control;

namespace Sharper.C.Control
{

public static class ExecStrategyModule
{
    public interface IExecStrategy
    {
        Func<A> Exec<A>(Func<A> f);
    }

    public static IExecStrategy ExecInline { get; } =
        new InlineExecStrategy();

    public static IExecStrategy ExecTask { get; } =
        new TaskExecStrategy(TaskScheduler.Default);

    public static IExecStrategy ExecDefault(IExecStrategy x = null)
    =>
        x ?? ExecInline;

    public static IExecStrategy ExecTaskWith(TaskScheduler scheduler)
    =>
        new TaskExecStrategy(scheduler);

    private sealed class InlineExecStrategy
      : IExecStrategy
    {
        public Func<A> Exec<A>(Func<A> f)
        {
            var a = f();
            return () => a;
        }
    }

    private sealed class TaskExecStrategy
      : IExecStrategy
    {
        private readonly TaskScheduler scheduler;

        public TaskExecStrategy(TaskScheduler scheduler)
        {
            this.scheduler = scheduler;
        }

        public Func<A> Exec<A>(Func<A> f)
        {
            var t =
                Task.Factory.StartNew
                  ( f
                  , CancellationToken.None
                  , TaskCreationOptions.None
                  , scheduler
                  );
            return () => t.Result;
        }
    }
}

}
