namespace AsyncWriterResult

[<AutoOpen>]
module WriterCE =
    type WriterBuilder() =
        member __.Return(x) = Writer.retn x
        member __.ReturnFrom(m: Writer<'w, 't>) = m
        member __.Bind(m, f) = Writer.bind m f
        member __.Zero() = __.Return()
        member __.BindReturn(x, f) = Writer.map f x
        member __.MergeSources(x, y) = Writer.zip x y

    let writer = WriterBuilder()
