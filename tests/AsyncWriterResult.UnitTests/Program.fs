module Program

open Expecto

[<EntryPoint>]
let main args =
    Tests.runTestsInAssemblyWithCLIArgs [ No_Spinner ] args
