using System;
using System.Collections.Generic;
using System.IO;
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
			//var path = @"D:\Dropbox\Repo\enyimmemcached2\Core\bin\Debug\Enyim.Caching.Core.dll";
			//var path = @"D:\Dropbox\repo\enyimmemcached2\Memcached\bin\Debug\Enyim.Caching.Memcached.dll";
			var path = typeof(Target.CombinedTests).Assembly.Location;

			var module = ModuleDefinition.ReadModule(path, new ReaderParameters { AssemblyResolver = new AR(path) });

			new Weavers.LogToWeaver
			{
				ModuleDefinition = module,
				LogInfo = Console.WriteLine,
				LogWarning = Console.WriteLine,
				LogError = Console.WriteLine
			}.Execute();
			new Weavers.EventSourceWeaver
			{
				ModuleDefinition = module,
				LogInfo = Console.WriteLine,
				LogWarning = Console.WriteLine,
				LogError = Console.WriteLine
			}.Execute();

			module.Write("d:\\out.dll");
		}
	}

	class AR : DefaultAssemblyResolver
	{
		public AR(string path)
		{
			var dir = Path.GetDirectoryName(path);
			foreach (var dll in Directory.GetFiles(dir, "*.dll"))
				RegisterAssembly(AssemblyDefinition.ReadAssembly(dll));
		}

		//public AssemblyDefinition Resolve(string fullName)
		//{
		//	return AssemblyDefinition.ReadAssembly(Path.Combine(path, fullName));
		//}

		//public AssemblyDefinition Resolve(AssemblyNameReference name)
		//{
		//	var file = Path.Combine(path, name.Name + ".dll");

		//	return File.Exists(file) ? AssemblyDefinition.ReadAssembly(file) : null;
		//}

		//public AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
		//{
		//	throw new NotImplementedException();
		//}

		//public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
		//{
		//	throw new NotImplementedException();
		//}
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
