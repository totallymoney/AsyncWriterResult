namespace AsyncWriterResult

open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module AsyncResult =
    let bindError f = Async.map (Result.bindError f)