using System;
using Sharper.C.Data;

namespace Sharper.C.Control
{

using static EitherModule;
using static ErrorModule;
using static ExecStrategyModule;
using static PromiseModule;
using static TrampolineModule;
using static UnsafeFutureModule;
using static UnitModule;

public static class FutureModule
{
    public sealed class Future<A>
    {
        internal readonly UnsafeFuture<Either<Exception, A>> fa;

        internal Future(UnsafeFuture<Either<Exception, A>> fa)
        {
            this.fa = fa;
        }

        public Promise<Either<Exception, A>> Eval(IExecStrategy x = null)
        =>
            fa.Eval(x);

        public Future<B> FlatMap<B>(Func<A, Future<B>> f)
        =>
            new Future<B>
              ( fa.FlatMap
                  ( ea =>
                        ea.Match
                          ( e => UnsafeNow(Error<B>(e))
                          , a =>
                                Try(() => f(a))
                                .Match
                                  ( e => UnsafeNow(Error<B>(e))
                                  , b => b.fa
                                  )
                          )
                  )
              );

        public Future<B> Map<B>(Func<A, B> f)
        =>
            new Future<B>(fa.Map(ea => ea.FlatMap(a => Try(() => f(a)))));

        public Unit Run
          ( Func<Either<Exception, A>, Trampoline<Unit>> k
          , IExecStrategy x = null
          )
        =>
            fa.Run(k, x);

        public Future<B> Select<B>(Func<A, B> f)
        =>
            Map(f);

        public Future<C> SelectMany<B, C>(Func<A, Future<B>> f, Func<A, B, C> g)
        =>
            FlatMap(a => f(a).Map(b => g(a, b)));
    }

    public static Future<A> Async<A>
      ( Action<Action<Either<Exception, A>>> listen
      )
    =>
        new Future<A>
          ( UnsafeAsync<Either<Exception, A>>
              ( k => ToFunc(listen)(a => Done(k(a)))
              )
          );

    public static Future<A> Delay<A>(Func<A> a)
    =>
        Suspend(() => Now(a()));

    public static Future<A> Now<A>(A a)
    =>
        new Future<A>(UnsafeNow(Result(a)));

    public static Future<A> Suspend<A>(Func<Future<A>> f)
    =>
        new Future<A>(UnsafeSuspend(() => f().fa));

    public static Future<Unit> Raise(Exception e)
    =>
        new Future<Unit>(UnsafeNow(Error<Unit>(e)));

    public static Future<A> Recover<A>
      ( Func<Exception, Future<A>> f
      , Future<A> fa
      )
    =>
        new Future<A>
          ( fa.fa.FlatMap
              ( ea =>
                    ea.Match
                      ( e => f(e).fa
                      , a => UnsafeNow(Result(a))
                      )
              )
          );
}

}
