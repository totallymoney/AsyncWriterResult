namespace AsyncWriterResult

[<RequireQualifiedAccess>]
module Task =

    let retn x = task { return x }

    let map f m =
        task {
            let! x = m
            return f x
        }

    let bind f m =
        task {
            let! x = m
            return! f x
        }

    let apply f m =
        task {
            let! unwrappedF = f
            let! x = m
            return unwrappedF x
        }

    let zip left right =
        bind (fun l -> bind (fun r -> retn (l, r)) right) left
        