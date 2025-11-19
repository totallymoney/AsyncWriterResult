#!/usr/bin/env -S dotnet fsi

#r "nuget: Fun.Build, 1.0.3"

open System
open System.IO
open Fun.Build

let (</>) a b = Path.Combine(a, b)
let sln = __SOURCE_DIRECTORY__ </> "AsyncWriterResult.sln"
let config = "Release"
let nupkgs = __SOURCE_DIRECTORY__ </> "nupkgs"

let envVar name =
    Environment.GetEnvironmentVariable(name)
    |> Option.ofObj
    |> Option.bind (fun v -> if String.IsNullOrWhiteSpace(v) then None else Some v)

let buildVersion = envVar "BUILD_VERSION"

let versionProperty =
    match buildVersion with
    | None -> "-p:Version=2.0.0"
    | Some v -> $"-p:Version=%s{v}"

pipeline "ci" {
    description "Main pipeline used for CI"

    stage "lint" {
        run "dotnet tool restore"
        run $"dotnet fantomas --check {__SOURCE_FILE__} src docs"
    }

    stage "build" {
        run $"dotnet restore {sln}"
        run $"dotnet build {sln} -c {config} --no-restore"
    }

    stage "test" {
        run
            $"dotnet run --project tests/AsyncWriterResult.UnitTests/AsyncWriterResult.UnitTests.fsproj -c {config} --no-build"
    }

    stage "pack" { run $"dotnet pack {sln} -c {config} -p:PackageOutputPath=\"%s{nupkgs}\" {versionProperty}" }

    stage "docs" {
        run $"dotnet publish src/AsyncWriterResult/AsyncWriterResult.fsproj -c {config} -f net8.0 --no-build"
        run $"dotnet fsdocs build --properties Configuration={config} --output output --eval --strict"
    }

    runIfOnlySpecified false
}

pipeline "docs" {
    description "Build the documentation (default)"

    stage "build" {
        run "dotnet tool restore"
        run $"dotnet restore {sln}"
        run $"dotnet build {sln} -c {config} --no-restore"
        run $"dotnet publish src/AsyncWriterResult/AsyncWriterResult.fsproj -c {config} -f net8.0 --no-build"
        run $"dotnet fsdocs build --properties Configuration={config} --eval --strict"
    }

    runIfOnlySpecified true
}

pipeline "docs:watch" {
    description "Watch and rebuild the documentation site"

    stage "build" {
        run $"dotnet restore {sln}"
        run $"dotnet build {sln} -c {config} --no-restore"
        run $"dotnet publish src/AsyncWriterResult/AsyncWriterResult.fsproj -c {config} -f net8.0 --no-build"
    }

    stage "watch" { run "dotnet fsdocs watch --eval --clean" }
    runIfOnlySpecified true
}

tryPrintPipelineCommandHelp ()
