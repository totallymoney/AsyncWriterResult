namespace AsyncWriterResult

[<RequireQualifiedAccess>]
module Task =

    let retn x = task { return x }
