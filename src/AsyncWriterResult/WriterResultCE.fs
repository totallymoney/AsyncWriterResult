namespace AsyncWriterResult

[<AutoOpen>]
module WriterResultCE =
    type WriterResultBuilder() =
        member __.Return(x) = WriterResult.retn x
        member __.ReturnFrom(m: Writer<'w, Result<'a, 'b>>) = m
        member __.Bind(m, f) = WriterResult.bind f m
        member __.Zero() = __.Return()
        member __.BindReturn(x, f) = WriterResult.map f x
        member __.MergeSources(x, y) = WriterResult.zip x y
        member __.Source(x: Writer<'w, Result<'a, 'b>>) = x

    let writerResult = WriterResultBuilder()

[<AutoOpen>]
module WriterResultBuilderExtensions =
    type WriterResultBuilder with
        member __.Source(x: Result<'a, 'b>) = x |> Writer.retn
        member __.Source(x: Writer<'w, 't>) = x |> Writer.map Ok
        