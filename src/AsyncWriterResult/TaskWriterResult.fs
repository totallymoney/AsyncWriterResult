namespace AsyncWriterResult

open System.Threading.Tasks
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module TaskWriterResult =

    let retn x = x |> WriterResult.retn |> Task.retn

    let returnError e = Error e |> TaskWriter.retn

    let map f = f |> WriterResult.map |> Task.map

    let eitherMap fok ferr =
        Result.eitherMap fok ferr |> Writer.map |> Task.map

    let bind f m =
        task {
            let! w = m
            let r, logs1 = Writer.run w

            match r with
            | Ok a ->
                let! ww = f a
                let b, logs2 = Writer.run ww
                return Writer <| fun () -> b, logs1 @ logs2
            | Error e -> return Writer <| fun () -> Error e, logs1
        }

    let bindError f = TaskWriter.map (Result.bindError f)

    let apply f m =
        task {
            let! uf = f
            let! um = m
            let r1, logs1 = Writer.run uf
            let r2, logs2 = Writer.run um

            match r1, r2 with
            | Ok g, Ok h -> return Writer <| fun () -> Ok(g h), logs1 @ logs2
            | Error e1, _ -> return Writer <| fun () -> Error e1, logs1 @ logs2
            | _, Error e2 -> return Writer <| fun () -> Error e2, logs1 @ logs2
        }

    let write log =
        task { return Writer(fun () -> Result.retn (), [ log ]) }

    let mapError e m =
        task {
            let! w = m
            let r, logs = Writer.run w
            return Writer <| fun () -> Result.mapError e r, logs
        }

    let traverseResultM f list =

        let (>>=) x f = bind f x

        let cons head tail = head :: tail

        let folder head tail =
            f head >>= (fun h -> tail >>= (fun t -> retn (cons h t)))

        List.foldBack folder list (retn [])

    let collect (tasks: TaskWriterResult<_, _, _> seq) =
        Task.WhenAll tasks |> Task.map (List.ofArray >> WriterResult.collect)

    let zip left right =
        Task.zip left right |> Task.map (fun (r1, r2) -> WriterResult.zip r1 r2)

    let ignore x = map ignore x
