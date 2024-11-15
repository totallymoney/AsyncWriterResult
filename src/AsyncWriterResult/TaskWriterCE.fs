namespace AsyncWriterResult

open FsToolkit.ErrorHandling
open AsyncWriterResult
open System.Threading.Tasks

[<AutoOpen>]
module TaskWriterCE =
    type TaskWriterBuilder() =
        member __.Return(x) = TaskWriter.retn x
        member __.ReturnFrom(m: Task<Writer<'w, 'a>>) = m
        member __.Bind(m, f) = TaskWriter.bind f m
        member __.Zero() = __.Return()
        member __.BindReturn(x, f) = TaskWriter.map f x
        member __.MergeSources(x, y) = TaskWriter.zip x y
        member __.Source(x: Task<Writer<'w, 'a>>) = x

    let taskWriter = TaskWriterBuilder()

[<AutoOpen>]
module TaskWriterBuilderExtensions =
    type TaskWriterBuilder with
        member __.Source(x: Writer<'w, 't>) = x |> Task.singleton
        member __.Source(x: Task<'a>) = x |> Task.map Writer.retn
        