module TaskWriterResultTests

open Expecto
open Expecto.Flip
open AsyncWriterResult

let ceTests =
    [ test "and! should run in parallel" {
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
          |> fun x -> x.Result
          |> ignore

          Expect.equal "" [1; 3; 4; 2] acc
      } ]

[<Tests>]
let tests =
    testList "TaskWriterResult" [
        testList "Helpers" []
        testList "CE" ceTests
        testList "Operators" []
    ]
