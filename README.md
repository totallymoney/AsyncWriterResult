# AsyncWriterResult

Combine async workflows with structured logging and error handling in F#. Track what happens in your async code without the usual plumbing.

## Installation

Use any of the following options depending on your setup.

- Dotnet CLI (recommended)
  - Add to a project: `dotnet add package AsyncWriterResult`
  - Or edit your `.fsproj` and include: `<PackageReference Include="AsyncWriterResult" Version="*" />`

- Paket
  - Add to dependencies: `paket add AsyncWriterResult`
  - Or in `paket.dependencies`: `nuget AsyncWriterResult`
  - Then run `paket install` and reference in your project file if needed.

- F# scripts (.fsx)
  - Reference directly from NuGet: `#r "nuget: AsyncWriterResult"`

## Quick Start

### Writing async code that logs its work

```fsharp
open AsyncWriterResult

type User = { Id: int; Name: string }

let fetchUserWithLogs userId =
    asyncWriter {
        do! Writer.write ($"[INFO] Fetching user {userId}")

        // Simulate API call
        let! user =
            async {
                do! Async.Sleep 200
                return { Id = userId; Name = "Alice" }
            }

        do! Writer.write ($"[INFO] Got user: {user.Name}")
        return user
    }

// Run and get both the value and the log trail
let user, logs =
    fetchUserWithLogs 123
    |> Async.RunSynchronously
    |> Writer.run
```

### When things can fail

```fsharp
open System.IO
open AsyncWriterResult

let parseConfigFile filename =
    asyncWriterResult {
        do! Writer.write ($"[INFO] Reading config from {filename}")

        if not (File.Exists filename) then
            do! Writer.write ($"[ERROR] File not found: {filename}")
            return! Error ($"Config file '{filename}' doesn't exist")

        let! content = async { return File.ReadAllText filename }
        do! Writer.write ($"[INFO] Read {content.Length} characters")

        if content.StartsWith "{" then
            do! Writer.write "[INFO] Valid JSON detected"
            return content
        else
            do! Writer.write "[ERROR] Invalid JSON format"
            return! Error "Invalid config format"
    }

let result, configLogs =
    parseConfigFile "missing.json"
    |> Async.RunSynchronously
    |> Writer.run
```

### Running things in parallel

```fsharp
open AsyncWriterResult

let checkServiceHealth (url: string) =
    async {
        do! Async.Sleep 100
        return if url.Contains "api" then "healthy" else "degraded"
    }

let healthCheck () =
    asyncWriterResult {
        do! Writer.write "[START] Health check initiated"

        let! apiStatus = checkServiceHealth "https://api.example.com"
        and! dbStatus = checkServiceHealth "https://db.example.com"
        and! cacheStatus = checkServiceHealth "https://cache.example.com"

        do! Writer.write ($"[STATUS] API: {apiStatus}")
        do! Writer.write ($"[STATUS] Database: {dbStatus}")
        do! Writer.write ($"[STATUS] Cache: {cacheStatus}")

        if apiStatus = "healthy" && dbStatus = "healthy" && cacheStatus = "healthy" then
            do! Writer.write "[OK] All systems operational"
            return "All systems GO"
        else
            do! Writer.write "[WARN] Some services degraded"
            return "Partial outage"
    }
```

### Processing lists with detailed logging

```fsharp
open AsyncWriterResult

type Order = { OrderId: string; Amount: decimal }

let processOrders orders =
    asyncWriterResult {
        do! Writer.write ($"[START] Processing {List.length orders} orders")
        let mutable total = 0m

        for order in orders do
            do! Writer.write ($"[PROCESS] Order {order.OrderId}: ${order.Amount}")
            if order.Amount <= 0m then
                do! Writer.write ($"[SKIP] Invalid amount for {order.OrderId}")
            else
                total <- total + order.Amount
                do! Writer.write ($"[OK] Added ${order.Amount} (running total: ${total})")

        do! Writer.write ($"[COMPLETE] Processed batch. Total: ${total}")
        return total
    }
```

### Composing operations

```fsharp
open AsyncWriterResult

let validateInput (input: string) =
    asyncWriter {
        do! Writer.write ($"[VALIDATE] Checking '{input}'")
        if input.Length > 3 then
            do! Writer.write "[VALIDATE] Input valid"
            return input.ToUpper()
        else
            do! Writer.write "[VALIDATE] Too short!"
            return ""
    }

let processData (data: string) =
    asyncWriter {
        do! Writer.write ($"[PROCESS] Working with '{data}'")
        let result = data.Replace("TEST", "PROD")
        do! Writer.write ($"[PROCESS] Transformed to '{result}'")
        return result
    }

let pipeline input =
    input
    |> validateInput
    |> AsyncWriter.bind processData
```

## How to contribute

*Imposter syndrome disclaimer*: I want your help. No really, I do.

There might be a little voice inside that tells you're not ready; that you need to do one more tutorial, or learn another framework, or write a few more blog posts before you can help me with this project.

I assure you, that's not the case.

This project has some clear Contribution Guidelines and expectations that you can [read here](CONTRIBUTING.md).

The contribution guidelines outline the process that you'll need to follow to get a patch merged. By making expectations and process explicit, I hope it will make it easier for you to contribute.

And you don't just have to write code. You can help out by writing documentation, tests, or even by giving feedback about this work. (And yes, that includes giving feedback about the contribution guidelines.)

Thank you for contributing!

## Contributing and copyright

The library is available under [MIT license](LICENSE.md), which allows modification and redistribution for both commercial and non-commercial purposes.

Please note that this project is released with a [Contributor Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project, you agree to abide by its terms.
