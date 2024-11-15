namespace AsyncWriterResult

open FsToolkit.ErrorHandling
open AsyncWriterResult

[<AutoOpen>]
module AsyncWriterCE =
    type AsyncWriterBuilder() =
        member __.Return(x) = AsyncWriter.retn x
        member __.ReturnFrom(m: Async<Writer<'w, 'a>>) = m
        member __.Bind(m, f) = AsyncWriter.bind f m
        member __.Zero() = __.Return()
        member __.BindReturn(x, f) = AsyncWriter.map f x
        member __.MergeSources(x, y) = AsyncWriter.zip x y
        member __.Source(x: Async<Writer<'w, 'a>>) = x

    let asyncWriter = AsyncWriterBuilder()

[<AutoOpen>]
module AsyncWriterBuilderExtensions =
    type AsyncWriterBuilder with
        member __.Source(x: Writer<'w, 't>) = x |> Async.singleton
        member __.Source(x: Async<'a>) = x |> Async.map Writer.retn
        