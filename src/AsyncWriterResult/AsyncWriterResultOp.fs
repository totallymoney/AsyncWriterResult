namespace AsyncWriterResult.Operator.AsyncWriterResult

open AsyncWriterResult

/// Operators for working with the AsyncWriterResult type
[<AutoOpen>]
module AsyncWriterResult =
    /// AsyncWriterResult.map
    let (<!>) f x = AsyncWriterResult.map f x

    /// AsyncWriterResult.apply
    let (<*>) f x = AsyncWriterResult.apply f x

    /// AsyncWriterResult.bind
    let (>>=) x f = AsyncWriterResult.bind f x
