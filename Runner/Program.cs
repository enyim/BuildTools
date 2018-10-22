using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Enyim.Build;
using Mono.Cecil;
using NDesk.Options;

namespace Runner
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
			args = @"-s D:\Repo\BuildTools\Target\bin\Debug\Target.dll -o d:\lofasz.dll -w Enyim.Build.Rewriters.LogTo.dll".Split(' ');
			args = @"-s D:\Repo\BuildTools\Target\bin\Debug\Target.dll -o d:\lofasz.dll -w Enyim.Build.Rewriters.EventSource.dll".Split(' ');

			try
			{
				var logger = new ColoredConsoleLogger();
				var options = new Options();
				if (options.Parse(args))
				{
					var weaver = new Weaver(Assembly.LoadFile(options.Weaver.FullName), logger);
					weaver.SetProperties(options.Properties);
					var result = weaver.Rewrite(options.Source.FullName);

					Save(result, options);
				}
			}
			catch (Exception e)
			{
				ConsoleHelper.ColoredWriteLine(ConsoleColor.Red, e.Message);
			}
		}

		private static void Save(ModuleDefinition module, Options options)
		{
			var key = options.KeyFile == null ? null : new StrongNameKeyPair(options.KeyFile.OpenRead());
			var target = (options.Target ?? options.Source).FullName;

			module.Write(target, new WriterParameters
			{
				StrongNameKeyPair = key,
				WriteSymbols = module.HasSymbols
			});
		}

		//static void Main2(string[] args)
		//{
		//	//var path = @"D:\Dropbox\Repo\enyimmemcached2\Core\bin\Debug\Enyim.Caching.Core.dll";
		//	//var path = @"D:\Dropbox\Repo\enyimmemcached2\Core\bin\Release\Enyim.Caching.Core.dll";
		//	var path = @"D:\Dropbox\repo\enyimmemcached2\Memcached\bin\Release\Enyim.Caching.Memcached.dll";
		//	//var path = @"D:\Dropbox\repo\enyimmemcached2\Memcached\bin\Release\Enyim.Caching.Memcached.dll";
		//	//var path = typeof(Target.CombinedTests).Assembly.Location;

		//	var module = ModuleDefinition.ReadModule(path, new ReaderParameters { AssemblyResolver = new WeaverAssembyResolver(path) });

		//	new Weavers.LogToWeaver
		//	{
		//		ModuleDefinition = module,
		//		LogInfo = Console.WriteLine,
		//		LogWarning = Console.WriteLine,
		//		LogError = Console.WriteLine
		//	}.Execute();
		//	new Weavers.EventSourceWeaver
		//	{
		//		ModuleDefinition = module,
		//		LogInfo = Console.WriteLine,
		//		LogWarning = Console.WriteLine,
		//		LogError = Console.WriteLine
		//	}.Execute();

		//	module.Write("d:\\out.dll");
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
