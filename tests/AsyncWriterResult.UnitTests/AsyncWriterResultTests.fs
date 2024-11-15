module AsyncWriterResultTests

open Expecto
open Expecto.Flip
open AsyncWriterResult

let ceTests =
    [ test "and! should run in parallel" {
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

          Expect.equal "" [1; 3; 4; 2] acc
      } ]

[<Tests>]
let tests =
    testList "AsyncWriterResult" [
        testList "Helpers" []
        testList "CE" ceTests
        testList "Operators" []
    ]
