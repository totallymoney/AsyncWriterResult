namespace AsyncWriterResult.Operator.WriterResult

open AsyncWriterResult

/// Operators for working with the WriterResult type
[<AutoOpen>]
module WriterResult =
    /// WriterResult.map
    let (<!>) f x = WriterResult.map f x

    /// WriterResult.apply
    let (<*>) f x = WriterResult.apply f x

    /// WriterResult.bind
    let (>>=) x f = WriterResult.bind f x
