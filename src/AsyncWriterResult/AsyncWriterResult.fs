namespace AsyncWriterResult

open FsToolkit.ErrorHandling

type AsyncWriterResult<'ok, 'error, 'log> = Async<Writer<'log list, Result<'ok, 'error>>>

[<RequireQualifiedAccess>]
module AsyncWriterResult =

    let retn x = x |> WriterResult.retn |> Async.retn

    let map f = f |> WriterResult.map |> Async.map

    // let bind (f:'a -> Async<Writer<'b list, Result<'c,'d>>>) (m:Async<Writer<'b list, Result<'a,'d>>>) : Async<Writer<'b list, Result<'c,'d>>> = async {
    let bind f m =
        async {
            let! w = m
            let r, logs1 = Writer.run w

            match r with
            | Ok a ->
                let! ww = f a
                let b, logs2 = Writer.run ww
                return Writer <| fun () -> b, logs1 @ logs2
            | Error e -> return Writer <| fun () -> Error e, logs1
        }

    let apply f m =
        async {
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
        async { return Writer(fun () -> Result.retn (), [ log ]) }

    let mapError e m =
        async {
            let! w = m
            let (r, logs) = Writer.run w
            return Writer <| fun () -> Result.mapError e r, logs
        }

    let private errMsg m (e: exn) = Error(sprintf "%s: %s" m e.Message)

    let tryTo desc f =
        f
        >> Async.Catch
        >> Async.map (function
            | Choice1Of2 a -> Ok a
            | Choice2Of2 e when (e :? System.AggregateException) -> errMsg desc e.InnerException
            | Choice2Of2 e -> errMsg desc e)
        >> Async.map Writer.retn

    let traverseResultM f list =

        let (>>=) x f = bind f x

        let cons head tail = head :: tail

        let folder head tail =
            f head
            >>= (fun h -> tail >>= (fun t -> retn (cons h t)))

        List.foldBack folder list (retn [])

    let collect list =
        Async.Parallel list
        |> Async.map (List.ofArray >> WriterResult.collect)

    let zip left right =
        Async.zip left right
        |> Async.map (fun (r1, r2) -> WriterResult.zip r1 r2)
