namespace AsyncWriterResult

open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module TaskWriter =

    let retn a = Writer.retn a |> Task.retn

    let map f = f |> Writer.map |> Task.map