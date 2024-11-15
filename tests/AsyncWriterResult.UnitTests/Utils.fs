[<AutoOpen>]
module Utils

open AsyncWriterResult

let createWriter item logs =
    item
    |> Writer.retn
    |> Writer.mapLogs (fun _ -> logs)
