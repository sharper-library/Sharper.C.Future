using System;
using Sharper.C.Data;

namespace Sharper.C.Control
{

using static ExecStrategyModule;
using static PromiseModule;
using static TrampolineModule;
using static UnitModule;

public static class UnsafeFutureModule
{
    public abstract class UnsafeFuture<A>
    {
        public abstract UnsafeFuture<B> FlatMap<B>(Func<A, UnsafeFuture<B>> f);
        protected abstract Unit DoRun(Func<A, Trampoline<Unit>> k);
        protected abstract UnsafeFuture<A> DoStep();
        protected abstract bool IsSynchronous { get; }

        public Promise<A> Eval(IExecStrategy x = null)
        {
            var p = MkPromise<A>();
            Run(a => Done(p.Fulfill(a)), x);
            return p;
        }

        public UnsafeFuture<B> Map<B>(Func<A, B> f)
        =>
            FlatMap(a => UnsafeNow(f(a)));

        public Unit Run(Func<A, Trampoline<Unit>> k, IExecStrategy x = null)
        =>
            ExecDefault(x).Exec(() => Step().DoRun(k))();

        public UnsafeFuture<A> Step()
        {
            var fa = this;
            Console.WriteLine("a");
            while (fa.IsSynchronous)
            {
                Console.WriteLine("b");
                fa = fa.DoStep();
            }
            Console.WriteLine("c");
            return fa;
        }

        protected X Unreachable<X>()
        {
            throw new Exception("Should not be reachable");
        }

        internal UnsafeFuture<A> Self
        =>
            this;
    }

    public static UnsafeFuture<A> UnsafeNow<A>(A a)
    =>
        new Now<A>(a);

    internal static UnsafeFuture<A> UnsafeAsync<A>
      ( Func<Func<A, Trampoline<Unit>>, Unit> listen
      )
    =>
        new Async<A>(listen);

    private static UnsafeFuture<B> UnsafeBindAsync<A, B>
      ( Func<Func<A, Trampoline<Unit>>, Unit> listen
      , Func<A, UnsafeFuture<B>> f
      )
    =>
        new BindAsync<A, B>(listen, f);

    private static UnsafeFuture<B> UnsafeBindSuspend<A, B>
      ( Func<UnsafeFuture<A>> thunk
      , Func<A, UnsafeFuture<B>> f
      )
    =>
        new BindSuspend<A, B>(thunk, f);

    internal static UnsafeFuture<A> UnsafeSuspend<A>(Func<UnsafeFuture<A>> a)
    =>
        new Suspend<A>(a);

    private sealed class Async<A>
      : UnsafeFuture<A>
    {
        public Func<Func<A, Trampoline<Unit>>, Unit> Listen { get; }

        public Async(Func<Func<A, Trampoline<Unit>>, Unit> listen)
        {
            Listen = listen;
        }

        public override UnsafeFuture<B> FlatMap<B>(Func<A, UnsafeFuture<B>> f)
        =>
            UnsafeBindAsync(Listen, f);

        protected override Unit DoRun(Func<A, Trampoline<Unit>> k)
        =>
            Listen(k);

        protected override UnsafeFuture<A> DoStep()
        =>
            Self;

        protected override bool IsSynchronous
        =>
            false;
    }

    private sealed class BindAsync<A, B>
      : UnsafeFuture<B>
    {
        public Func<Func<A, Trampoline<Unit>>, Unit> Listen { get; }
        public Func<A, UnsafeFuture<B>> F { get; }

        public BindAsync
          ( Func<Func<A, Trampoline<Unit>>, Unit> listen
          , Func<A, UnsafeFuture<B>> f
          )
        {
            Listen = listen;
            F = f;
        }

        public override UnsafeFuture<C> FlatMap<C>(Func<B, UnsafeFuture<C>> f)
        =>
            UnsafeSuspend
              ( () => UnsafeBindAsync(Listen, b => F(b).FlatMap(f))
              );

        protected override Unit DoRun(Func<B, Trampoline<Unit>> k)
        =>
            Listen
              ( x =>
                    Suspend
                      ( () => Done(F(x)).Map(y => y.Run(k))
                      )
              );

        protected override UnsafeFuture<B> DoStep()
        =>
            Self;

        protected override bool IsSynchronous
        =>
            false;
    }

    private sealed class BindSuspend<A, B>
      : UnsafeFuture<B>
    {
        public Func<UnsafeFuture<A>> Thunk { get; }
        public Func<A, UnsafeFuture<B>> F { get; }

        public BindSuspend
          ( Func<UnsafeFuture<A>> thunk
          , Func<A, UnsafeFuture<B>> f
          )
        {
            Thunk = thunk;
            F = f;
        }

        public override UnsafeFuture<C> FlatMap<C>(Func<B, UnsafeFuture<C>> f)
        =>
            UnsafeSuspend
              ( () => UnsafeBindSuspend(Thunk, b => F(b).FlatMap(f))
              );

        protected override Unit DoRun(Func<B, Trampoline<Unit>> k)
        =>
            Unreachable<Unit>();

        protected override UnsafeFuture<B> DoStep()
        =>
            Thunk().FlatMap(F);

        protected override bool IsSynchronous
        =>
            true;
    }

    private sealed class Now<A>
      : UnsafeFuture<A>
    {
        public A Value { get; }

        public Now(A a)
        {
            Value = a;
        }

        public override UnsafeFuture<B> FlatMap<B>(Func<A, UnsafeFuture<B>> f)
        =>
            UnsafeSuspend(() => f(Value));

        protected override Unit DoRun(Func<A, Trampoline<Unit>> k)
        =>
            k(Value).Eval();

        protected override UnsafeFuture<A> DoStep()
        =>
            Self;

        protected override bool IsSynchronous
        =>
            false;
    }

    private sealed class Suspend<A>
      : UnsafeFuture<A>
    {
        public Func<UnsafeFuture<A>> Thunk { get; }

        public Suspend(Func<UnsafeFuture<A>> thunk)
        {
            Thunk = thunk;
        }

        public override UnsafeFuture<B> FlatMap<B>(Func<A, UnsafeFuture<B>> f)
        =>
            UnsafeBindSuspend(Thunk, f);

        protected override Unit DoRun(Func<A, Trampoline<Unit>> k)
        =>
            Unreachable<Unit>();

        protected override UnsafeFuture<A> DoStep()
        =>
            Thunk();

        protected override bool IsSynchronous
        =>
            true;
    }
}

}
