using System;
using System.Threading.Tasks;
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

        public async Task<A> RunAsTask()
        {
            var ea = await Task.Factory.StartNew(() => Eval(ExecInline).Wait);
            return ResultOrThrow(ea);
        }
    }

    public static Future<A> Async<A>
      ( Action<Action<Either<Exception, A>>> listen
      )
    =>
        new Future<A>
          ( UnsafeAsync<Either<Exception, A>>
              ( k => ToFunc(listen)(a => k(a).Eval())
              )
          );

    public static Future<A> Delay<A>(Func<A> a)
    =>
        Suspend(() => Now(a()));

    public static Future<Unit> DelayAction(Action a)
    =>
        Suspend(() => Now(ToFunc(a)()));

    public static Future<A> Now<A>(A a)
    =>
        new Future<A>(UnsafeNow(Result(a)));

    public static Future<A> Suspend<A>(Func<Future<A>> f)
    =>
        new Future<A>(UnsafeSuspend(() => f().fa));

    public static Future<Unit> Raise(Exception e)
    =>
        new Future<Unit>(UnsafeNow(Error<Unit>(e)));

    public static Future<A> Raise<A>(Exception e)
    =>
        new Future<A>(UnsafeNow(Error<A>(e)));

    public static Future<A> Recover<E, A>
      ( Func<E, Future<A>> f
      , Future<A> fa
      )
      where E : Exception
    =>
        new Future<A>
          ( fa.fa.FlatMap
              ( ea =>
                    ea.Match
                      ( e => e is E ? f(e as E).fa : UnsafeNow(Error<A>(e))
                      , a => UnsafeNow(Result(a))
                      )
              )
          );

    public static Future<A> Recover<A>
      ( Func<Exception, Future<A>> f
      , Future<A> fa
      )
    =>
        Recover<Exception, A>(f, fa);

    public static Future<A> Await<A>(Task<A> t)
    =>
        Async<A>
          ( async k =>
            {
                try
                {
                    var a = await t;
                    k(Result(a));
                }
                catch (Exception e)
                {
                    k(Error<A>(e));
                }
            }
          );
}

}
