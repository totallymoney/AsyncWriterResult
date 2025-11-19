namespace AsyncWriterResult

[<RequireQualifiedAccess>]
module Writer =

    let run<'w, 't> (Writer w) : 't * 'w list = w ()

    let retn a = Writer <| fun () -> a, []

    let map f m =
        let a, w = run m
        Writer <| fun () -> f a, w

    let mapLogs f m =
        let a, w = run m
        Writer <| fun () -> a, f w

    let eitherMap logMapper itemMapper m =
        let a, w = run m
        Writer <| fun () -> itemMapper a, logMapper w

    let bind m f =
        let a, logs1 = run m
        let b, logs2 = run (f a)
        Writer <| fun () -> b, logs1 @ logs2

    let apply f m =
        let unwrappedF, logs1 = run f
        let unwrappedA, logs2 = run m

        Writer <| fun () -> unwrappedF unwrappedA, logs1 @ logs2

    let collect l =
        Writer
        <| fun () -> List.fold (fun (items, logs) (item, log) -> item :: items, log :: logs) ([], []) (List.map run l)

    let zip (left: Writer<_, _>) (right: Writer<_, _>) =
        bind left (fun l -> bind right (fun r -> retn (l, r)))

    let write x = Writer <| fun () -> (), [ x ]

    let ignore x = map ignore x
