#r "../packages/FAKE/tools/FakeLib.dll"

open Fake

let private guessBranch =
    match (environVarOrNone "APPVEYOR_REPO_BRANCH") with
    | None ->
        let ok,msg, _ = Fake.Git.CommandHelper.runGitCommand "." "rev-parse --abbrev-ref HEAD"
        match ok with
        | true -> msg |> Seq.head
        | false -> "local"
    | Some v -> v

let private guessCommitHash =
    let ok,msg, _= Fake.Git.CommandHelper.runGitCommand "." "log --pretty=format:%h -1"
    match ok with
    | true -> msg |> Seq.head
    | false -> environVar "APPVEYOR_REPO_COMMIT"

let inline (<+>) list sep = list |> Seq.where(fun p-> p <> null) |> String.concat sep

/// version+branch.sha.counter
let private getInformalVersion version =
    let sha = guessCommitHash
    let branch = guessBranch
    let counter = environVar "APPVEYOR_BUILD_NUMBER"

    [version; [branch; sha; counter] <+> "."] <+> "+"

let parseSolutionVersion p =
    let userVersion = ReadLine (p @@ "VERSION")
    let version = System.Text.RegularExpressions.Regex.Match(userVersion, "^\d+.\d+(.\d+)?").Value
    let informalVersion = getInformalVersion userVersion

    (version, informalVersion)

let private sendToAppVeyor args =
    ExecProcess (fun info ->
        info.FileName <- "appveyor"
        info.Arguments <- args) (System.TimeSpan.MaxValue)
     |> ignore

let tryPublishBuildInfo version =
    if (getEnvironmentVarAsBool "APPVEYOR") then
        sendToAppVeyor <| sprintf "UpdateBuild -Version \"%s\"" version

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
