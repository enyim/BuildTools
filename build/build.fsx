#r "../packages/FAKE/tools/FakeLib.dll"
#load "./utils.fsx"

open Fake
open Fake.Testing.XUnit2

// configuration

let configuration = getBuildParamOrDefault "configuration" "Release"
let outputPath = getBuildParamOrDefault "output" "output"
let pushTo = getBuildParamOrDefault "pushTo" "myget"
let feeds = dict [ ("myget", "https://www.myget.org/F/enyimmemcached2/api/v2/package");
                   ("nuget", null) ]
//

let solutionDir = System.IO.Path.GetFullPath(__SOURCE_DIRECTORY__ + "\\..")
let outputDir = (solutionDir </> outputPath) + "/"
let solutionPath = solutionDir @@ "BuildTools.sln"

let projectVersion, projectInformalVersion = Utils.parseSolutionVersion solutionDir
let packageVersion = projectInformalVersion.Split('+').[0]

/// do a nuget package restore
Target "RestorePackages" (fun _ -> solutionPath |> RestoreMSSolutionPackages(id))

Target "Submodules" (fun _ -> Fake.Git.CommandHelper.runGitCommand "." "submodule update --init --recursive" |> ignore)

/// build the solution
Target "Build" (fun _ ->
    let buildParams defaults =
        {defaults with Verbosity = Some(Minimal)
                       Targets = ["Build"]
                       Properties =
                           ["Configuration", configuration
                            "Platform", "Any CPU"
                            "ILMergeEnabled", "True"
                            "SolutionDir", solutionDir
                            "ProjectVersion", projectVersion
                            "CollectOutputs", "True"
                            "OutputRoot", outputDir
                            "ProjectInformalVersion", projectInformalVersion]}
    Utils.tryPublishBuildInfo projectInformalVersion
    build buildParams solutionPath |> DoNothing)

/// build nuget packages
Target "Pack" (fun _ ->
    ensureDirectory outputDir

    !!(solutionDir + "/**/*.nuspec")
    |> Seq.iter(fun nuspec ->
           NuGetPackDirectly (fun p ->
               {p with Publish = false
                       WorkingDir = (DirectoryName nuspec)
                       OutputPath = outputDir
                       Version = packageVersion
                       Properties = ["Configuration", configuration]}) nuspec))

let guessApiKey pushTo =
    if hasBuildParam "apikey"
        then getBuildParam "apikey"
        else match (environVarOrNone "NUGET_API_KEY") with
                    | None -> ReadLine (solutionDir @@ (pushTo + ".apikey"))
                    | Some v -> v

let doPush pushTo =
    let apiKey = guessApiKey pushTo

    !! (outputDir + "\\*." + packageVersion + ".nupkg")
    |> Seq.map GetMetaDataFromPackageFile
    |> Seq.iter (fun f -> NuGetPublish (fun p ->
                            {p with Publish = true
                                    PublishUrl = feeds.[pushTo]
                                    AccessKey = apiKey
                                    Version = packageVersion
                                    OutputPath = outputDir
                                    WorkingDir = solutionDir
                                    Project = f.Id}))

    DeleteDir outputDir

// push packages to myget/nuget
Target "Push" (fun _ -> doPush pushTo)
Target "Myget" (fun _ -> doPush "myget")
Target "Nuget" (fun _ -> doPush "nuget")

"RestorePackages"
    ==> "Build"
    ==> "Pack"

"RestorePackages" <== Dependency "Submodules"
"Push" <== Dependency "Pack"
"Nuget" <== Dependency "Pack"
"Myget" <== Dependency "Pack"

RunParameterTargetOrDefault "target" "Pack"

(*

    Copyright (c) Attila Kiskó, enyim.com

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

        http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

*)
