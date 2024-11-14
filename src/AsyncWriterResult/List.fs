namespace AsyncWriterResult

[<RequireQualifiedAccess>]
module List =
    (* Writer *)
    let rec private traverseWriterA'
        (state: Writer<_, _>)
        (f: _ -> Writer<_, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> Writer.map List.rev
        | x :: xs ->
            let w =
                writer {
                    let! ys = state
                    let! y = f x
                    return y :: ys
                }

            traverseWriterA' w f xs
    
    let traverseWriterA (f: 'a -> Writer<'log, 'b>) (xs: 'a list) : Writer<'log, 'b list> =
        traverseWriterA' (Writer.retn []) f xs

    let sequenceWriter xs = traverseWriterA id xs

    (* Async Writer *)
    let rec private traverseAsyncWriterA'
        (state: AsyncWriter<_, _>)
        (f: _ -> AsyncWriter<_, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> AsyncWriter.map List.rev
        | x :: xs ->
            let w =
                asyncWriter {
                    let! ys = state
                    let! y = f x
                    return y :: ys
                }

            traverseAsyncWriterA' w f xs

    let traverseAsyncWriterA (f: 'a -> AsyncWriter<'log, 'b>) (xs: 'a list) : AsyncWriter<'log, 'b list> =
        traverseAsyncWriterA' (AsyncWriter.retn []) f xs

    let sequenceAsyncWriter xs = traverseAsyncWriterA id xs

    (* Task Writer *)
    let rec private traverseTaskWriterA'
        (state: TaskWriter<_, _>)
        (f: _ -> TaskWriter<_, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> TaskWriter.map List.rev
        | x :: xs ->
            let w =
                taskWriter {
                    let! ys = state
                    let! y = f x
                    return y :: ys
                }

            traverseTaskWriterA' w f xs

    let traverseTaskWriterA (f: 'a -> TaskWriter<'log, 'b>) (xs: 'a list) : TaskWriter<'log, 'b list> =
        traverseTaskWriterA' (TaskWriter.retn []) f xs

    let sequenceTaskWriter xs = traverseTaskWriterA id xs

    (* Writer Result *)
    let rec private traverseWriterResultM'
        (state: WriterResult<_, _, _>)
        (f: _ -> WriterResult<_, _, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> WriterResult.map List.rev
        | x :: xs ->
            writer {
                let! r =
                    writerResult {
                        let! ys = state
                        let! y = f x
                        return y :: ys
                    }

                match r with
                | Ok _ -> return! traverseWriterResultM' (Writer.retn r) f xs
                | Error _ -> return r
            }

    let traverseWriterResultM (f: 'a -> WriterResult<'b, 'error, 'log>) (xs: 'a list) : WriterResult<'b list, 'error, 'log> =
        traverseWriterResultM' (WriterResult.retn []) f xs

    let sequenceWriterResultM xs = traverseWriterResultM id xs


    let rec private traverseWriterResultA'
        (state: WriterResult<_, _, _>)
        (f: _ -> WriterResult<_, _, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> WriterResult.eitherMap List.rev List.rev
        | x :: xs ->
            writer {
                let! ys = state
                let! y = f x

                match ys, y with
                | Ok ys, Ok y -> return! traverseWriterResultA' (WriterResult.retn (y :: ys)) f xs
                | Error errs, Error e ->
                    return! traverseWriterResultA' (WriterResult.returnError (e :: errs)) f xs
                | Ok _, Error e ->
                    return! traverseWriterResultA' (WriterResult.returnError [ e ]) f xs
                | Error e, Ok _ -> return! traverseWriterResultA' (WriterResult.returnError e) f xs
            }

    let traverseWriterResultA (f: 'a -> WriterResult<'b, 'error, 'log>) (xs: 'a list) : WriterResult<'b list, 'error list, 'log> =
        traverseWriterResultA' (WriterResult.retn []) f xs

    let sequenceWriterResultA xs = traverseWriterResultA id xs

    (* Async Writer Result *)
    let rec private traverseAsyncWriterResultM'
        (state: AsyncWriterResult<_, _, _>)
        (f: _ -> AsyncWriterResult<_, _, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> AsyncWriterResult.map List.rev
        | x :: xs ->
            asyncWriter {
                let! r =
                    asyncWriterResult {
                        let! ys = state
                        let! y = f x
                        return y :: ys
                    }

                match r with
                | Ok _ -> return! traverseAsyncWriterResultM' (AsyncWriter.retn r) f xs
                | Error _ -> return r
            }

    let traverseAsyncWriterResultM (f: 'a -> AsyncWriterResult<'b, 'error, 'log>) (xs: 'a list) : AsyncWriterResult<'b list, 'error, 'log> =
        traverseAsyncWriterResultM' (AsyncWriterResult.retn []) f xs

    let sequenceAsyncWriterResultM xs = traverseAsyncWriterResultM id xs


    let rec private traverseAsyncWriterResultA'
        (state: AsyncWriterResult<_, _, _>)
        (f: _ -> AsyncWriterResult<_, _, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> AsyncWriterResult.eitherMap List.rev List.rev
        | x :: xs ->
            asyncWriter {
                let! ys = state
                let! y = f x

                match ys, y with
                | Ok ys, Ok y -> return! traverseAsyncWriterResultA' (AsyncWriterResult.retn (y :: ys)) f xs
                | Error errs, Error e ->
                    return! traverseAsyncWriterResultA' (AsyncWriterResult.returnError (e :: errs)) f xs
                | Ok _, Error e ->
                    return! traverseAsyncWriterResultA' (AsyncWriterResult.returnError [ e ]) f xs
                | Error e, Ok _ -> return! traverseAsyncWriterResultA' (AsyncWriterResult.returnError e) f xs
            }

    let traverseAsyncWriterResultA (f: 'a -> AsyncWriterResult<'b, 'error, 'log>) (xs: 'a list) : AsyncWriterResult<'b list, 'error list, 'log> =
        traverseAsyncWriterResultA' (AsyncWriterResult.retn []) f xs

    let sequenceAsyncWriterResultA xs = traverseAsyncWriterResultA id xs

    (* Task Writer Result *)
    let rec private traverseTaskWriterResultM'
        (state: TaskWriterResult<_, _, _>)
        (f: _ -> TaskWriterResult<_, _, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> TaskWriterResult.map List.rev
        | x :: xs ->
            taskWriter {
                let! r =
                    taskWriterResult {
                        let! ys = state
                        let! y = f x
                        return y :: ys
                    }

                match r with
                | Ok _ -> return! traverseTaskWriterResultM' (TaskWriter.retn r) f xs
                | Error _ -> return r
            }

    let traverseTaskWriterResultM (f: 'a -> TaskWriterResult<'b, 'error, 'log>) (xs: 'a list) : TaskWriterResult<'b list, 'error, 'log> =
        traverseTaskWriterResultM' (TaskWriterResult.retn []) f xs

    let sequenceTaskWriterResultM xs = traverseTaskWriterResultM id xs


    let rec private traverseTaskWriterResultA'
        (state: TaskWriterResult<_, _, _>)
        (f: _ -> TaskWriterResult<_, _, _>)
        xs
        =
        match xs with
        | [] ->
            state
            |> TaskWriterResult.eitherMap List.rev List.rev
        | x :: xs ->
            taskWriter {
                let! ys = state
                let! y = f x

                match ys, y with
                | Ok ys, Ok y -> return! traverseTaskWriterResultA' (TaskWriterResult.retn (y :: ys)) f xs
                | Error errs, Error e ->
                    return! traverseTaskWriterResultA' (TaskWriterResult.returnError (e :: errs)) f xs
                | Ok _, Error e ->
                    return! traverseTaskWriterResultA' (TaskWriterResult.returnError [ e ]) f xs
                | Error e, Ok _ -> return! traverseTaskWriterResultA' (TaskWriterResult.returnError e) f xs
            }

    let traverseTaskWriterResultA (f: 'a -> TaskWriterResult<'b, 'error, 'log>) (xs: 'a list) : TaskWriterResult<'b list, 'error list, 'log> =
        traverseTaskWriterResultA' (TaskWriterResult.retn []) f xs

    let sequenceTaskWriterResultA xs = traverseTaskWriterResultA id xs