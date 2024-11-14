namespace AsyncWriterResult

open System.Threading.Tasks
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module Task =

    let retn x = task { return x }

    let runSynchronously (x: Task<_>) = x.Result

    let catchResult task =
        Task.catch task |> Task.map Result.ofChoice
