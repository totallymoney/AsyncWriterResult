namespace AsyncWriterResult

[<AutoOpen>]
module Types =
    open System.Threading.Tasks

    type Writer<'log, 'item> = Writer of (unit -> 'item * 'log list)

    type AsyncWriter<'log, 'item> = Async<Writer<'log, 'item>>

    type TaskWriter<'log, 'item> = Task<Writer<'log, 'item>>

    type WriterResult<'ok, 'error, 'log> = Writer<'log, Result<'ok, 'error>>

    type AsyncWriterResult<'ok, 'error, 'log> = Async<Writer<'log, Result<'ok, 'error>>>

    type TaskWriterResult<'ok, 'error, 'log> = Task<Writer<'log, Result<'ok, 'error>>>