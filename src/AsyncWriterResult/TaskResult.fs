namespace AsyncWriterResult

open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module TaskResult =
    let bindError f = Task.map (Result.bindError f)
