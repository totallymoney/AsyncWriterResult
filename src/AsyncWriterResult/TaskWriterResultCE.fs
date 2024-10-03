namespace AsyncWriterResult

open FsToolkit.ErrorHandling
open System.Threading.Tasks

[<AutoOpen>]
module TaskWriterResultCE =

    type TaskWriterResultBuilder() =
        member __.Return(x) = Task.retn x
        member __.ReturnFrom(m: Task<Writer<'w, Result<'a, 'b>>>) = m
        member __.Bind(m, f) = Task.bind f m
        member __.Zero() = __.Return()
        member __.BindReturn(x, f) = Task.map f x
        member __.MergeSources(x, y) = Task.zip x y
        member __.Source(x: Task<Writer<'w, Result<'a, 'b>>>) = x
        member __.Source(x: Async<Writer<'w, Result<'a, 'b>>>) = x |> Async.StartAsTask

    let taskWriterResult = TaskWriterResultBuilder()

    [<AutoOpen>]
    module TaskWriterResultBuilderExtensions =
        type TaskWriterResultBuilder with
            member __.Source(x: Result<'a, 'b>) = x |> TaskWriter.retn
            member __.Source(x: Writer<'w, 't>) = x |> Writer.map Ok |> Task.retn
            member __.Source(x: Task<'t>) = x |> Task.map WriterResult.retn
            member __.Source(x: Async<'t>) = x |> Async.StartAsTask |> Task.map WriterResult.retn

    [<AutoOpen>]
    module TaskWriterResultBuilderExtensionsHighPriority =
        type TaskWriterResultBuilder with
            member __.Source(x: Writer<'w, Result<'a, 'b>>) = x |> Task.retn
            member __.Source(x: Task<Result<'a, 'b>>) = x |> Task.map Writer.retn
            member __.Source(x: Task<Writer<'w, 't>>) = x |> TaskWriter.map Result.retn
            member __.Source(x: Async<Result<'a, 'b>>) = x |> Async.StartAsTask |> Task.map Writer.retn
            member __.Source(x: Async<Writer<'w, 't>>) = x |> Async.StartAsTask |> TaskWriter.map Result.retn