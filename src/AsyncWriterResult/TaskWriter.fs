namespace AsyncWriterResult

open System.Threading.Tasks
open FsToolkit.ErrorHandling

[<RequireQualifiedAccess>]
module TaskWriter =

    let retn a = Writer.retn a |> Task.retn

    let map f = f |> Writer.map |> Task.map

    let mapLogs f = Task.map (Writer.mapLogs f)

    let bind (f: 'a -> TaskWriter<'log, 'b>) (x: TaskWriter<'log, 'a>) : TaskWriter<'log, 'b> =
        task {
            let! writerA = x

            let a, logsA = Writer.run writerA

            let! writerB = f a

            let b, logsB = Writer.run writerB

            return Writer <| fun () -> b, logsA @ logsB
        }

    let bindError f = Task.map (Result.bindError f)

    let mapBind (binder: 'a -> Writer<'w list, 'b>) (input: Task<Writer<'w list, 'a>>) =
        input |> Task.map (fun x -> Writer.bind x binder)

    let zip left right =
        Task.zip left right
        |> Task.map (fun (r1, r2) -> Writer.zip r1 r2)

    let ignore x = map ignore x