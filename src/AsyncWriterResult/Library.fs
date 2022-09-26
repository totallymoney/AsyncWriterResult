[<AutoOpen>]
module AsyncWriterResult

open System.Threading.Tasks

module Async =

    let retn x = async { return x }

    let map f m =
        async {
            let! x = m
            return f x
        }

    let bind f m =
        async {
            let! x = m
            return! f x
        }

    let apply f m =
        async {
            let! unwrappedF = f
            let! x = m
            return unwrappedF x
        }


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


module Result =

    let retn = Ok

    let apply f m =
        match f, m with
        | Ok f, Ok x -> Ok(f x)
        | Error errs, _ -> Error errs
        | _, Error errs -> Error errs

    let (<!>) = Result.map
    let (<*>) = apply

    let traverseResultM f list =

        // define the monadic functions
        let (>>=) x f = Result.bind f x

        // define a "cons" function
        let cons head tail = head :: tail

        // right fold over the list
        let folder head tail =
            f head
            >>= (fun h -> tail >>= (fun t -> retn (cons h t)))

        List.foldBack folder list (retn [])



type Writer<'w, 't> = Writer of (unit -> ('t * 'w))

module Writer =

    let run<'w, 't> (Writer w) : 't * 'w = w ()

    let retn a = Writer <| fun () -> a, []

    let map f m =
        let (a, w) = run m
        Writer <| fun () -> f a, w

    let bind m f =
        let (a, logs1) = run m
        let (b, logs2) = run (f a)
        Writer <| fun () -> b, logs1 @ logs2

    let apply f m =
        let (unwrappedF, logs1) = run f
        let (unwrappedA, logs2) = run m

        Writer
        <| fun () -> unwrappedF unwrappedA, logs1 @ logs2

    let collect l =
        Writer
        <| fun () -> List.fold (fun (items, logs) (item, log) -> item :: items, log :: logs) ([], []) (List.map run l)

    let write log = Writer <| fun () -> (), [ log ]


module WriterResult =

    let retn x = x |> Result.retn |> Writer.retn

    let map f = f |> Result.map |> Writer.map

    let error e = Error e |> Writer.retn

    let mapError e m =
        let (r, logs) = Writer.run m
        Writer <| fun () -> Result.mapError e r, logs

    // let bind (f:'a -> Writer<'b list, Result<'c,'d>>) (m:Writer<'b list, Result<'a,'d>>) : Writer<'b list, Result<'c,'d>> =
    let bind f m =
        let (r, logs1) = Writer.run m

        match r with
        | Ok a ->
            let (b, logs2) = Writer.run (f a)
            Writer <| fun () -> b, logs1 @ logs2
        | Error e -> Writer <| fun () -> Error e, logs1

    let apply f m =
        let (r1, logs1) = Writer.run f
        let (r2, logs2) = Writer.run m

        match r1, r2 with
        | Ok g, Ok h -> Writer <| fun () -> Ok(g h), logs1 @ logs2
        | Error e1, _ -> Writer <| fun () -> Error e1, logs1 @ logs2
        | _, Error e2 -> Writer <| fun () -> Error e2, logs1 @ logs2

    let collect list =
        let collectResult head tail =
            head
            |> Result.bind (fun h ->
                tail
                |> Result.bind (fun t -> Result.retn (h :: t)))

        let folder (items, logs) (item, log) = collectResult item items, log @ logs

        Writer
        <| fun () -> List.fold folder (Result.retn [], []) (List.map Writer.run list)

    let write log =
        Writer <| fun () -> Result.retn (), [ log ]


type AsyncWriterResult<'ok, 'error, 'log> = Async<Writer<'log list, Result<'ok, 'error>>>


[<AutoOpen>]
module AsyncWriterResult =

    let retn x = x |> WriterResult.retn |> Async.retn

    let map f = f |> WriterResult.map |> Async.map

    // let bind (f:'a -> Async<Writer<'b list, Result<'c,'d>>>) (m:Async<Writer<'b list, Result<'a,'d>>>) : Async<Writer<'b list, Result<'c,'d>>> = async {
    let bind f m =
        async {
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
        async {
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


module AsyncWriter =

    let retn a = Writer.retn a |> Async.retn


module TaskWriter =

    let retn a = Writer.retn a |> Task.retn



type ResultBuilder() =
    member __.Return(x) = Result.retn x
    member __.ReturnFrom(m: Result<_, _>) = m
    member __.Bind(m, f) = Result.bind f m
    member __.Zero() = Error()

let result = ResultBuilder()


type WriterBuilder() =
    member __.Return(x) = Writer.retn x
    member __.ReturnFrom(m: Writer<'w, 't>) = m
    member __.Bind(m, f) = Writer.bind m f
    member __.Zero() = __.Return()

let writer = WriterBuilder()


type WriterResultBuilder() =
    member __.Return(x) = WriterResult.retn x
    member __.ReturnFrom(m: Writer<'w, Result<'a, 'b>>) = m
    member __.Bind(m, f) = WriterResult.bind f m
    member __.Zero() = __.Return()

let writerResult = WriterResultBuilder()


type AsyncWriterResultBuilder() =
    member __.Return(x) = AsyncWriterResult.retn x
    member __.ReturnFrom(m: Async<Writer<'w, Result<'a, 'b>>>) = m
    member __.Bind(m, f) = AsyncWriterResult.bind f m
    member __.Zero() = __.Return()

let asyncWriterResult = AsyncWriterResultBuilder()


type TaskWriterResultBuilder() =
    member __.Return(x) = TaskWriterResult.retn x
    member __.ReturnFrom(m: Task<Writer<'w, Result<'a, 'b>>>) = m
    member __.Bind(m, f) = TaskWriterResult.bind f m
    member __.Zero() = __.Return()

let taskWriterResult = TaskWriterResultBuilder()
