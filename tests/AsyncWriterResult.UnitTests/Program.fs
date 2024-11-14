module Program

open Expecto
open Tests

[<EntryPoint>]
let main args =
    runTestsWithArgs defaultConfig args tests
