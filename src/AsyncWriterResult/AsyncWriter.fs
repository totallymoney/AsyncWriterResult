namespace AsyncWriterResult

open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module AsyncWriter =

    let retn a = Writer.retn a |> Async.retn

    let map f = f |> Writer.map |> Async.map

    let bind (f: 'a -> AsyncWriter<'log, 'b>) (x: AsyncWriter<'log, 'a>) : AsyncWriter<'log, 'b> =
        async {
            let! writerA = x

            let a, logsA = Writer.run writerA

            let! writerB = f a

            let b, logsB = Writer.run writerB

            return Writer <| fun () -> b, logsA @ logsB
        } // TODO: test

    let zip left right =
        Async.zip left right
        |> Async.map (fun (r1, r2) -> Writer.zip r1 r2)