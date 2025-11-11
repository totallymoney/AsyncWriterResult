(**
---
title: AsyncWriterResult
category: docs
index: 0
---
*)
#r "../src/AsyncWriterResult/bin/Release/net8.0/publish/AsyncWriterResult.dll"

(**
# AsyncWriterResult

Combine async workflows with logging and error handling in F#. Track what's happening in your async code without the mess.

## Quick Start
*)

open AsyncWriterResult

(**
### Writing async code that logs its work

Let's say you're fetching data from an API and want to track what's happening:
*)

type User = { Id: int; Name: string }

let fetchUserWithLogs userId =
    asyncWriter {
        do! Writer.write $"[INFO] Fetching user {userId}"

        // Simulate API call
        let! user =
            async {
                do! Async.Sleep 200 // network delay
                return { Id = userId; Name = "Alice" }
            }

        do! Writer.write $"[INFO] Got user: {user.Name}"
        return user
    }

// Run it and see what happened
let user, fetchLogs =
    fetchUserWithLogs 123
    |> Async.RunSynchronously
    |> Writer.run

printfn $"User: %A{user}"
printfn $"What happened: %A{fetchLogs}"

(*** include-output ***)


(**
### When things can fail

Real code fails. Here's how to handle errors while keeping the logs:
*)

let parseConfigFile filename =
    asyncWriterResult {
        do! Writer.write $"[INFO] Reading config from {filename}"

        // Check if file exists
        if not (System.IO.File.Exists filename) then
            do! Writer.write $"[ERROR] File not found: {filename}"
            return! Error $"Config file '{filename}' doesn't exist"

        // Try to read and parse
        let! content = async { return System.IO.File.ReadAllText filename }

        do! Writer.write $"[INFO] Read {content.Length} characters"

        // Parse JSON (simplified)
        if content.StartsWith "{" then
            do! Writer.write "[INFO] Valid JSON detected"
            return content
        else
            do! Writer.write "[ERROR] Invalid JSON format"
            return! Error "Invalid config format"
    }

// Try with a missing file
let configResult, configLogs =
    parseConfigFile "missing.json"
    |> Async.RunSynchronously
    |> Writer.run

match configResult with
| Ok config -> printfn $"Config loaded: %s{config}"
| Error (msg: string) -> printfn $"Failed: %s{msg}"

printfn $"Log trail:\n%A{configLogs}"

(*** include-output ***)

(**
### Running things in parallel

Fetch multiple things at once with `and!` - they run in parallel but logs stay organized:
*)

let checkServiceHealth (url: string) =
    async {
        do! Async.Sleep 100 // simulate network call

        return
            if url.Contains "api" then
                "healthy"
            else
                "degraded"
    }

let healthCheck () =
    asyncWriterResult {
        do! Writer.write "[START] Health check initiated"

        let! apiStatus = checkServiceHealth "https://api.example.com"
        and! dbStatus = checkServiceHealth "https://db.example.com"
        and! cacheStatus = checkServiceHealth "https://cache.example.com"

        do! Writer.write $"[STATUS] API: {apiStatus}"
        do! Writer.write $"[STATUS] Database: {dbStatus}"
        do! Writer.write $"[STATUS] Cache: {cacheStatus}"

        let allHealthy =
            apiStatus = "healthy"
            && dbStatus = "healthy"
            && cacheStatus = "healthy"

        if allHealthy then
            do! Writer.write "[OK] All systems operational"
            return "All systems GO"
        else
            do! Writer.write "[WARN] Some services degraded"
            return "Partial outage"
    }

let healthResult, healthLogs =
    healthCheck ()
    |> Async.RunSynchronously
    |> Writer.run

match healthResult with
| Ok status -> printfn $"Status: %s{status}"
| Error (_: string) -> printfn "Health check failed"

printfn $"\nHealth check log:\n%A{healthLogs}"

(*** include-output ***)


(**
### Processing lists with detailed logging

Track what happens to each item when processing collections:
*)

type Order = { OrderId: string; Amount: decimal }

let processOrders orders =
    asyncWriterResult {
        do! Writer.write $"[START] Processing {List.length orders} orders"
        let mutable total = 0m

        for order in orders do
            do! Writer.write $"[PROCESS] Order {order.OrderId}: ${order.Amount}"

            // Validate
            if order.Amount <= 0m then
                do! Writer.write $"[SKIP] Invalid amount for {order.OrderId}"
            else
                total <- total + order.Amount
                do! Writer.write $"[OK] Added ${order.Amount} (running total: ${total})"

        do! Writer.write $"[COMPLETE] Processed batch. Total: ${total}"
        return total
    }

let orders =
    [ { OrderId = "ORD-001"; Amount = 99.99m }
      { OrderId = "ORD-002"; Amount = 0m } // invalid
      { OrderId = "ORD-003"
        Amount = 150.00m } ]

let orderResult, orderLogs =
    processOrders orders
    |> Async.RunSynchronously
    |> Writer.run

match orderResult with
| Ok total -> printfn $"Total processed: $%.2f{total}"
| Error (e: string) -> printfn $"Processing failed: %s{e}"

printfn "\nProcessing log:"
orderLogs |> List.iter (printfn "  %s")

(*** include-output ***)

(**
## Composing operations

Chain operations together - logs flow through automatically:
*)

let validateInput (input: string) =
    asyncWriter {
        do! Writer.write $"[VALIDATE] Checking '{input}'"

        if String.length input > 3 then
            do! Writer.write "[VALIDATE] Input valid"
            return input.ToUpper()
        else
            do! Writer.write "[VALIDATE] Too short!"
            return ""
    }

let processData (data: string) =
    asyncWriter {
        do! Writer.write $"[PROCESS] Working with '{data}'"
        let result = data.Replace("TEST", "PROD")
        do! Writer.write $"[PROCESS] Transformed to '{result}'"
        return result
    }

let pipeline input =
    input
    |> validateInput
    |> AsyncWriter.bind processData

let finalResult, pipelineLogs =
    pipeline "test_data"
    |> Async.RunSynchronously
    |> Writer.run

printfn $"Result: %s{finalResult}"
printfn "\nPipeline trace:"
pipelineLogs |> List.iter (printfn "  %s")

(*** include-output ***)
