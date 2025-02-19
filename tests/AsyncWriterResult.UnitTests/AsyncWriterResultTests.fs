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
      }

      test "can be used in a for loop" {
          let aw =
              asyncWriterResult {
                  for i in [ 1; 2; 3; 4 ] do
                      do! Writer.write i
              }

          let _, actualWritten =
              aw
              |> Async.RunSynchronously
              |> Writer.run

          Expect.equal "written" [ 1; 2; 3; 4 ] actualWritten
      }
      
      test "can use if-return" {
          let aw x =
              asyncWriterResult {
                  if x then
                      do! Writer.write 1
                  return Writer.write 0
              }

          let _, actualWritten =
              aw true
              |> Async.RunSynchronously
              |> Writer.run

          Expect.equal "written" [ 1 ] actualWritten
      } ]

[<Tests>]
let tests =
    testList "AsyncWriterResult" [
        testList "Helpers" []
        testList "CE" ceTests
        testList "Operators" []
    ]
