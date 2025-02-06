namespace AsyncWriterResult

open System
open FsToolkit.ErrorHandling
open System.Threading.Tasks

[<AutoOpen>]
module AsyncWriterResultCE =

    type AsyncWriterResultBuilder() =
        member __.Return(x) = AsyncWriterResult.retn x
        member __.ReturnFrom(m: Async<Writer<'w, Result<'a, 'b>>>) = m
        member __.Bind(m, f) = AsyncWriterResult.bind f m
        member __.Zero() = __.Return()
        member __.BindReturn(x, f) = AsyncWriterResult.map f x
        member __.MergeSources(x, y) = AsyncWriterResult.zip x y
        member _.Delay(generator: unit -> Async<Writer<'log,Result<'ok, 'error>>>) : Async<Writer<'log,Result<'ok,'error>>> =
            async.Delay generator

        member inline this.Combine
            (
                computation1: Async<Writer<'log, Result<unit, 'error>>>,
                computation2: Async<Writer<'log, Result<'ok, 'error>>>
            ) : Async<Writer<'log, Result<'ok, 'error>>> =
            this.Bind (computation1, (fun () -> computation2))

        member inline _.TryFinally
            (computation: Async<Writer<'log,Result<'ok,'error>>>, [<InlineIfLambda>] compensation: unit -> unit)
            : Async<Writer<'log,Result<'ok,'error>>> =
            async.TryFinally(computation, compensation)

        member this.While
            (guard: unit -> bool, computation: Async<Writer<'log,Result<unit,'error>>>)
            : Async<Writer<'log,Result<unit,'error>>> =
            if not (guard ()) then
                this.Zero()
            else
                this.Bind(computation, (fun () -> this.While(guard, computation)))

        member inline this.Using
            (
                resource: 'disposable :> IDisposable,
                [<InlineIfLambda>] binder: 'disposable -> Async<Writer<'log,Result<unit,'error>>>
            ) : Async<Writer<'log,Result<unit,'error>>> =
            this.TryFinally(
                (binder resource),
                (fun () ->
                    if not (obj.ReferenceEquals(resource, null)) then
                        resource.Dispose()
                )
            )

        member inline this.For
            (sequence: #seq<'input>, binder: 'input -> Async<Writer<'log, Result<unit,'error>>>)
            : Async<Writer<'log,Result<unit, 'error>>> =
            this.Using(
                sequence.GetEnumerator(),
                fun enum -> this.While(enum.MoveNext, this.Delay(fun () -> binder enum.Current))
            )
        member __.Source(s: #seq<_>) : #seq<_> = s
        member __.Source(x: Async<Writer<'w, Result<'a, 'b>>>) = x
        member __.Source(x: Task<Writer<'w, Result<'a, 'b>>>) = x |> Async.AwaitTask

    let asyncWriterResult = AsyncWriterResultBuilder()

    [<AutoOpen>]
    module AsyncWriterResultBuilderExtensions =
        type AsyncWriterResultBuilder with
            member __.Source(x: Result<'a, 'b>) = x |> AsyncWriter.retn
            member __.Source(x: Writer<'w, 't>) = x |> Writer.map Ok |> Async.retn
            member __.Source(x: Async<'t>) = x |> Async.map WriterResult.retn
            member __.Source(x: Task<'t>) = x |> Async.AwaitTask |> Async.map WriterResult.retn

    [<AutoOpen>]
    module AsyncWriterResultBuilderExtensionsHighPriority =
        type AsyncWriterResultBuilder with
            member __.Source(x: Writer<'w, Result<'a, 'b>>) = x |> Async.retn
            member __.Source(x: Async<Result<'a, 'b>>) = x |> Async.map Writer.retn
            member __.Source(x: Async<Writer<'w, 't>>) = x |> AsyncWriter.map Result.retn
            member __.Source(x: Task<Result<'a, 'b>>) = x |> Async.AwaitTask |> Async.map Writer.retn
            member __.Source(x: Task<Writer<'w, 't>>) = x |> Async.AwaitTask |> AsyncWriter.map Result.retn
