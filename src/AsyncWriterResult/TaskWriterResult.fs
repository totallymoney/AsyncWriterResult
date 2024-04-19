namespace Task


open System.Threading.Tasks




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



module TaskWriter =

    let retn a = Writer.retn a |> Task.retn

    let map f = f |> Writer.map |> Task.map



type TaskWriterResult<'ok, 'error, 'log> = Task<Writer<'log list, Result<'ok, 'error>>>


module TaskWriterResult =

    let retn x = x |> WriterResult.retn |> Task.retn

    let map f = f |> WriterResult.map |> Task.map

    let bind f m =
        task {
            let! w = m
            let (r, logs1) = Writer.run w

            match r with
            | Ok a ->
                let! ww = f a
                let (b, logs2) = Writer.run ww
                return Writer <| fun () -> b, logs1 @ logs2
            | Error e -> return Writer <| fun () -> Error e, logs1
        }

    let apply f m =
        task {
            let! uf = f
            let! um = m
            let (r1, logs1) = Writer.run uf
            let (r2, logs2) = Writer.run um

            match r1, r2 with
            | Ok g, Ok h -> return Writer <| fun () -> Ok(g h), logs1 @ logs2
            | Error e1, _ -> return Writer <| fun () -> Error e1, logs1 @ logs2
            | _, Error e2 -> return Writer <| fun () -> Error e2, logs1 @ logs2
        }

    module Operators =

        let (<!>) = map
        let (>>=) = bind
        let (<*>) = apply

    let write log =
        task { return Writer(fun () -> Result.retn (), [ log ]) }

    let mapError e m =
        task {
            let! w = m
            let (r, logs) = Writer.run w
            return Writer <| fun () -> Result.mapError e r, logs
        }

    let traverseResultM f list =

        let (>>=) x f = bind f x

        let cons head tail = head :: tail

        let folder head tail =
            f head
            >>= (fun h -> tail >>= (fun t -> retn (cons h t)))

        List.foldBack folder list (retn [])

    let collect (tasks: TaskWriterResult<_, _, _> seq) =
        Task.WhenAll tasks
        |> Task.map (List.ofArray >> WriterResult.collect)

    let zip left right =
        Task.zip left right
        |> Task.map (fun (r1, r2) -> WriterResult.zip r1 r2)

    type TaskWriterResultBuilder() =
        member __.Return(x) = retn x
        member __.ReturnFrom(m: Task<Writer<'w, Result<'a, 'b>>>) = m
        member __.Bind(m, f) = bind f m
        member __.Zero() = __.Return()
        member __.BindReturn(x, f) = map f x
        member __.MergeSources(x, y) = zip x y
        member __.Source(x: Task<Writer<'w, Result<'a, 'b>>>) = x
        member __.Source(x: Async<Writer<'w, Result<'a, 'b>>>) = x |> Async.StartAsTask

    let taskWriterResult = TaskWriterResultBuilder()

    [<AutoOpen>]
    module TaskWriterResultBuilderExtensions =
        type TaskWriterResultBuilder with
            member __.Source(x: Result<'a, 'b>) = x |> TaskWriter.retn
            member __.Source(x: Writer<'w, 't>) = x |> Writer.map Ok |> Task.retn
            member __.Source(x: Task<'t>) = x |> Task.map WriterResult.retn
            member __.Source(x: Async<'t>) = x |> Async.StartAsTask |> Task.map WriterResult.retn

    [<AutoOpen>]
    module TaskWriterResultBuilderExtensionsHighPriority =
        type TaskWriterResultBuilder with
            member __.Source(x: Writer<'w, Result<'a, 'b>>) = x |> Task.retn
            member __.Source(x: Task<Result<'a, 'b>>) = x |> Task.map Writer.retn
            member __.Source(x: Task<Writer<'w, 't>>) = x |> TaskWriter.map Result.retn
            member __.Source(x: Async<Result<'a, 'b>>) = x |> Async.StartAsTask |> Task.map Writer.retn
            member __.Source(x: Async<Writer<'w, 't>>) = x |> Async.StartAsTask |> TaskWriter.map Result.retn