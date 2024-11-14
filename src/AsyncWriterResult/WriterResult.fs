namespace AsyncWriterResult

open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module WriterResult =

    let retn x = x |> Result.retn |> Writer.retn

    let returnError e = Error e |> Writer.retn

    let map x = Result.map x |> Writer.map

    let mapError f m =
        Writer.map (Result.mapError f) m

    let eitherMap fok ferr = Result.eitherMap fok ferr |> Writer.map

    // let bind (f:'a -> Writer<'b list, Result<'c,'d>>) (m:Writer<'b list, Result<'a,'d>>) : Writer<'b list, Result<'c,'d>> =
    let bind f m =
        let r, logs1 = Writer.run m

        match r with
        | Ok a ->
            let b, logs2 = Writer.run (f a)
            Writer <| fun () -> b, logs1 @ logs2
        | Error e -> Writer <| fun () -> Error e, logs1

    let apply f m =
        let r1, logs1 = Writer.run f
        let r2, logs2 = Writer.run m

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
