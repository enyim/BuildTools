using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Runner
{
	class Program
	{
		static void Main(string[] args)
		{
			var module = ModuleDefinition.ReadModule(typeof(Target.LogTests).Assembly.Location);

			new Weavers.LogToWeaver { ModuleDefinition = module, LogInfo = Console.WriteLine, Debug = true }.Execute();
			new Weavers.EventSourceWeaver
			{
				ModuleDefinition = module,
				LogInfo = Console.WriteLine,
				LogWarning = Console.WriteLine,
				LogError = Console.WriteLine,
				Debug = true
			}.Execute();

			module.Write("d:\\out.dll");
		}
	}
}

#region [ License information          ]

/* ************************************************************
 *
 *    Copyright (c) Attila Kiskó, enyim.com
 *
 *    Licensed under the Apache License, Version 2.0 (the "License");
 *    you may not use this file except in compliance with the License.
 *    You may obtain a copy of the License at
 *
 *        http://www.apache.org/licenses/LICENSE-2.0
 *
 *    Unless required by applicable law or agreed to in writing, software
 *    distributed under the License is distributed on an "AS IS" BASIS,
 *    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *    See the License for the specific language governing permissions and
 *    limitations under the License.
 *
 * ************************************************************/

#endregion
