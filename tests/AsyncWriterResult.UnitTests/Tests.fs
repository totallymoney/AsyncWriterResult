module Tests

open Expecto
open Task.TaskWriterResult

let tests =
    testList "Group of tests"
        [ test "asyncWriterResult and! should run in parallel" {
              let mutable acc : int list = []
              let append x = acc <- acc @ [x]
              
              asyncWriterResult {
                  let! _ =
                      async {
                          append 1
                          do! Async.Sleep 1500
                          append 2
                      }
                  and! _ =
                      async {
                          append 3
                          do! Async.Sleep 1000
                          append 4
                      }
                  return ()
              }
              |> Async.RunSynchronously
              |> ignore

              Expect.equal acc [1; 3; 4; 2] ""
          }

          test "taskWriterResult and! should run in parallel" {
              let mutable acc : int list = []
              let append x = acc <- acc @ [x]

              taskWriterResult {
                  let! _ =
                      task {
                          append 1
                          do! Async.Sleep 1500
                          append 2
                      }
                  and! _ =
                      task {
                          append 3
                          do! Async.Sleep 1000
                          append 4
                      }
                  return ()
              }
              |> _.Result
              |> ignore

              Expect.equal acc [1; 3; 4; 2] ""
          } ]
