namespace AsyncWriterResult.Operator.Writer

open AsyncWriterResult

/// Operators for working with the Writer type
[<AutoOpen>]
module Writer =
    /// Writer.map
    let (<!>) f x = Writer.map f x

    /// Writer.apply
    let (<*>) f x = Writer.apply f x

    /// Writer.bind
    let (>>=) x f = Writer.bind f x