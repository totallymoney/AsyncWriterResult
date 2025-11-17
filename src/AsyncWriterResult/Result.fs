namespace AsyncWriterResult

[<RequireQualifiedAccess>]
module Result =

    let retn x = Ok x

    let bindError f =
        function
        | Ok x -> Ok x
        | Error x -> f x
