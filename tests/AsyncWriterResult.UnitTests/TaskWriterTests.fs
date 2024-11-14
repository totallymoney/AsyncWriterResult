module TaskWriterTests

open Expecto
open Expecto.Flip
open AsyncWriterResult

let helperTests =
    [ test "bind" {
          let aw =
              taskWriter {
                  do! Writer.write 1
                  do! Writer.write 2
                  return 1
              }

          let binder input =
              Expect.equal "" 1 input

              taskWriter {
                  do! Writer.write 3
                  do! Writer.write 4
                  return 2
              }

          let actualValue, actualWritten =
              TaskWriter.bind binder aw
              |> Task.runSynchronously
              |> Writer.run

          Expect.equal "value" 2 actualValue
          Expect.equal "written" [1; 2; 3; 4] actualWritten
      } ]

[<Tests>]
let tests =
    testList "TaskWriter" [
        testList "Helpers" helperTests
        testList "CE" []
        testList "Operators" []
    ]
