module ListTests

open Expecto
open Expecto.Flip
open AsyncWriterResult

[<Tests>]
let tests =
    testList
        "List"
        [ test "traverseWriterA should keep all logs" {
              let input = [ 1, createWriter 1 [ 1; 2 ]; 2, createWriter 2 [ 2; 3 ] ]

              let actualItem, actualLogs = List.traverseWriterA snd input |> Writer.run

              Expect.equal "item" [ 1; 2 ] actualItem
              Expect.equal "logs" [ 1; 2; 2; 3 ] actualLogs
          }

          test "traverseWriterA should maintain log order" {
              let input =
                  [ 1, createWriter 1 [ 11; 12; 13; 00 ]; 2, createWriter 2 [ 21; 22; 23; 00 ] ]

              let actualItem, actualLogs = List.traverseWriterA snd input |> Writer.run

              Expect.equal "item" [ 1; 2 ] actualItem
              Expect.equal "logs" [ 11; 12; 13; 00; 21; 22; 23; 00 ] actualLogs
          } ]
