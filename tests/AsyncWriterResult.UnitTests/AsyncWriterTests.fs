module AsyncWriterTests

open Expecto
open Expecto.Flip
open AsyncWriterResult

let helperTests =
    [ test "bind" {
          let aw =
              asyncWriter {
                  do! Writer.write 1
                  do! Writer.write 2
                  return 1
              }

          let binder input =
              Expect.equal "" 1 input

              asyncWriter {
                  do! Writer.write 3
                  do! Writer.write 4
                  return 2
              }

          let actualValue, actualWritten =
              AsyncWriter.bind binder aw |> Async.RunSynchronously |> Writer.run

          Expect.equal "value" 2 actualValue
          Expect.equal "written" [ 1; 2; 3; 4 ] actualWritten
      } ]

[<Tests>]
let tests =
    testList "AsyncWriter" [ testList "Helpers" helperTests; testList "CE" []; testList "Operators" [] ]
