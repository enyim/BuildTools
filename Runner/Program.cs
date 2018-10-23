using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			//args = @"-s D:\Repo\BuildTools\Target\bin\Debug\Target.dll -o d:\lofasz.dll -r Enyim.Build.Rewriters.LogTo.dll -p A=1 -p B=2 -p C=3".Split(' ');
			args = @"-s D:\Repo\BuildTools\Target\bin\Debug\Target.dll -o d:\lofasz.dll -r Enyim.Build.Rewriters.EventSource.dll".Split(' ');

			try
			{
				ConsoleLoggerProvider.Install();

				var options = new Options();
				if (options.Parse(args))
				{
					new Rewriter().Rewrite(options);
				}
			}
			catch (Exception e)
			{
				ConsoleHelper.ColoredWriteLine(ConsoleColor.Red, e.Message);
			}

			if (Debugger.IsAttached)
				Console.ReadLine();
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
