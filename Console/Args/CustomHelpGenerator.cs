using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;
using McMaster.Extensions.CommandLineUtils.HelpText;

namespace Enyim.Build
{
	internal class CustomHelpGenerator : DefaultHelpTextGenerator
	{
		protected override void GenerateUsage(CommandLineApplication application, TextWriter output,
												IReadOnlyList<CommandArgument> visibleArguments,
												IReadOnlyList<CommandOption> visibleOptions,
												IReadOnlyList<CommandLineApplication> visibleCommands)
		{
			output.Write("Usage:");

			var stack = new Stack<string>();
			for (var cmd = application; cmd != null; cmd = cmd.Parent)
			{
				stack.Push(cmd.Name);
			}

			while (stack.Count > 0)
			{
				output.Write(' ');
				output.Write(stack.Pop());
			}

			foreach (var a in visibleArguments)
			{
				output.Write(" <");
				output.Write(a.Name);
				output.Write(">");
			}

			if (visibleOptions.Count > 0) output.Write(" [options]");
			if (visibleCommands.Count > 0) output.Write(" [command]");
			if (application.AllowArgumentSeparator) output.Write(" [[--] <arg>...]");

			output.WriteLine();
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
