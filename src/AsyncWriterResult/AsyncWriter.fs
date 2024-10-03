namespace AsyncWriterResult

open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module AsyncWriter =

    let retn a = Writer.retn a |> Async.retn

    let map f = f |> Writer.map |> Async.map
    