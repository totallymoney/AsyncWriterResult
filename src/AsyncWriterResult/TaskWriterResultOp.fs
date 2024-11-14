namespace AsyncWriterResult.Operator.TaskWriterResult

open AsyncWriterResult

/// Operators for working with the TaskWriterResult type
[<AutoOpen>]
module TaskWriterResult =
    /// TaskWriterResult.map
    let (<!>) f x = TaskWriterResult.map f x

    /// TaskWriterResult.apply
    let (<*>) f x = TaskWriterResult.apply f x

    /// TaskWriterResult.bind
    let (>>=) x f = TaskWriterResult.bind f x