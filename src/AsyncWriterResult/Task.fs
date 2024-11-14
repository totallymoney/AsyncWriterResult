namespace AsyncWriterResult

[<RequireQualifiedAccess>]
module Task =
    open System.Threading.Tasks

    let retn x = task { return x }

    let runSynchronously (x: Task<_>) = x.Result
