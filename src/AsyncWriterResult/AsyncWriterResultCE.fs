namespace AsyncWriterResult

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
