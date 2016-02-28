@echo off

if not exist "packages\fake" (
	rem 4.5.3 throws an excption during install
	build\nuget install fake -outputdirectory packages -excludeversion
)

packages\fake\tools\fake .\build\build.fsx %* 

::    Copyright (c) Attila Kiskó, enyim.com
::
::    Licensed under the Apache License, Version 2.0 (the "License");
::    you may not use this file except in compliance with the License.
::    You may obtain a copy of the License at
::
::        http://www.apache.org/licenses/LICENSE-2.0
::
::    Unless required by applicable law or agreed to in writing, software
::    distributed under the License is distributed on an "AS IS" BASIS,
::    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
::    See the License for the specific language governing permissions and
::    limitations under the License.
::
