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

    let zip c1 c2 =
        async {
            let! ct = Async.CancellationToken
            let x = Async.StartImmediateAsTask (c1, cancellationToken=ct)
            let y = Async.StartImmediateAsTask (c2, cancellationToken=ct)
            let! x' = Async.AwaitTask x
            let! y' = Async.AwaitTask y
            return x', y'
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

    let zip left right =
        match left, right with
        | Ok x1res, Ok x2res -> Ok(x1res, x2res)
        | Error e, _ -> Error e
        | _, Error e -> Error e


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

    let zip (left: Writer<_, _>) (right: Writer<_, _>) =
        bind left (fun l -> bind right (fun r -> retn (l, r)))

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

    let zip left right =
        Writer.zip left right
        |> Writer.map (fun (r1, r2) -> Result.zip r1 r2)

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

    let zip left right =
        Async.zip left right
        |> Async.map (fun (r1, r2) -> WriterResult.zip r1 r2)


module AsyncWriter =

    let retn a = Writer.retn a |> Async.retn

    let map f = f |> Writer.map |> Async.map


type ResultBuilder() =
    member __.Return(x) = Result.retn x
    member __.ReturnFrom(m: Result<_, _>) = m
    member __.Bind(m, f) = Result.bind f m
    member __.Zero() = Error()
    member __.BindReturn(x, f) = Result.map f x
    member __.MergeSources(x, y) = Result.zip x y

let result = ResultBuilder()


type WriterBuilder() =
    member __.Return(x) = Writer.retn x
    member __.ReturnFrom(m: Writer<'w, 't>) = m
    member __.Bind(m, f) = Writer.bind m f
    member __.Zero() = __.Return()
    member __.BindReturn(x, f) = Writer.map f x
    member __.MergeSources(x, y) = Writer.zip x y

let writer = WriterBuilder()


type WriterResultBuilder() =
    member __.Return(x) = WriterResult.retn x
    member __.ReturnFrom(m: Writer<'w, Result<'a, 'b>>) = m
    member __.Bind(m, f) = WriterResult.bind f m
    member __.Zero() = __.Return()
    member __.BindReturn(x, f) = WriterResult.map f x
    member __.MergeSources(x, y) = WriterResult.zip x y
    member __.Source(x: Writer<'w, Result<'a, 'b>>) = x

let writerResult = WriterResultBuilder()

[<AutoOpen>]
module WriterResultBuilderExtensions =
    type WriterResultBuilder with
        member __.Source(x: Result<'a, 'b>) = x |> Writer.retn
        member __.Source(x: Writer<'w, 't>) = x |> Writer.map Ok


type AsyncWriterResultBuilder() =
    member __.Return(x) = AsyncWriterResult.retn x
    member __.ReturnFrom(m: Async<Writer<'w, Result<'a, 'b>>>) = m
    member __.Bind(m, f) = AsyncWriterResult.bind f m
    member __.Zero() = __.Return()
    member __.BindReturn(x, f) = AsyncWriterResult.map f x
    member __.MergeSources(x, y) = AsyncWriterResult.zip x y
    member __.Source(x: Async<Writer<'w, Result<'a, 'b>>>) = x
    member __.Source(x: Task<Writer<'w, Result<'a, 'b>>>) = x |> Async.AwaitTask

let asyncWriterResult = AsyncWriterResultBuilder()

[<AutoOpen>]
module AsyncWriterResultBuilderExtensions =
    type AsyncWriterResultBuilder with
        member __.Source(x: Result<'a, 'b>) = x |> AsyncWriter.retn
        member __.Source(x: Writer<'w, 't>) = x |> Writer.map Ok |> Async.retn
        member __.Source(x: Async<'t>) = x |> Async.map WriterResult.retn
        member __.Source(x: Task<'t>) = x |> Async.AwaitTask |> Async.map WriterResult.retn

[<AutoOpen>]
module AsyncWriterResultBuilderExtensionsHighPriority =
    type AsyncWriterResultBuilder with
        member __.Source(x: Writer<'w, Result<'a, 'b>>) = x |> Async.retn
        member __.Source(x: Async<Result<'a, 'b>>) = x |> Async.map Writer.retn
        member __.Source(x: Async<Writer<'w, 't>>) = x |> AsyncWriter.map Result.retn
        member __.Source(x: Task<Result<'a, 'b>>) = x |> Async.AwaitTask |> Async.map Writer.retn
        member __.Source(x: Task<Writer<'w, 't>>) = x |> Async.AwaitTask |> AsyncWriter.map Result.retn
