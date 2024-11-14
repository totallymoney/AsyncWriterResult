namespace AsyncWriterResult

[<RequireQualifiedAccess>]
module Async =

    let zip c1 c2 =
        async {
            let! ct = Async.CancellationToken
            let x = Async.StartImmediateAsTask (c1, cancellationToken = ct)
            let y = Async.StartImmediateAsTask (c2, cancellationToken = ct)
            let! x' = Async.AwaitTask x
            let! y' = Async.AwaitTask y
            return x', y'
        }
