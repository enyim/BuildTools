#define EASY_DEBUG_OFF
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using McMaster.Extensions.CommandLineUtils;

namespace Enyim.Build
{
	internal static class Program
	{
		private static void Main(string[] args)
		{
#if EASY_DEBUG
			if (Debugger.IsAttached)
			{
				//const string Source = @"D:\Repo\BuildTools\Target\bin\Debug\Target.dll";
				const string Source = @"D:\Repo\BuildTools\TargetNetstd\bin\Debug\netstandard2.0\TargetNetstd.dll";

				args = $@"Enyim.Build.Rewriters.LogTo.dll {Source} --output:lofasz.dll -p A=1 -p:B=2 --property C==3 --symbols:portable".Split(' ');
			}
#endif

			ConsoleLoggerProvider.Install();
			CreateApp().Execute(args);

#if EASY_DEBUG
			if (Debugger.IsAttached) Console.ReadLine();
#endif
		}

		private static CommandLineApplication CreateApp()
		{
			var app = new CommandLineApplication
			{
				Name = "runner",
				HelpTextGenerator = new CustomHelpGenerator()
			};

			app.ValueParsers.Add(new FileInfoValueParser());
			app.HelpOption();
			app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);

			var rewriter = app.Argument<FileInfo>(
								"rewriter",
								"Path to the rewriter plugin dll")
								.IsRequired()
								.Accepts(v => v.ExistingFile());

			var source = app.Argument<FileInfo>(
								"source",
								"Path to source assembly to be rewritten")
								.IsRequired()
								.Accepts(v => v.ExistingFile());

			var target = app.Option<FileInfo>("-o | --output <path>",
								"The output path where the rewritten assembly will be copied to; " +
								"if not specified the source will be overwritten",
								CommandOptionType.SingleValue);

			var symbols = app.Option<DebugSymbolsKind>(
								"-s | --symbols <value>",
								$"If debug symbols should be emitted and in what format. ({String.Join(", ", Enum.GetNames(typeof(DebugSymbolsKind)))})", CommandOptionType.SingleValue)
								.Accepts(v => v.Enum<DebugSymbolsKind>(true));

			var props = app.Option(
								"-p | --property <value>",
								"Property values for the rewriter in the format of 'name=value'",
								CommandOptionType.MultipleValue)
								.Accepts(v => v.RegularExpression("^([^=]+)=(.+)"));

			app.OnExecute(() =>
			{
				var options = new Options
				{
					Rewriter = rewriter.ParsedValue,
					Source = source.ParsedValue,
					Target = target.HasValue() ? target.ParsedValue : source.ParsedValue,
					Symbols = symbols.ParsedValue,
					Properties =
					{
						from value in props.Values
						let index = value.IndexOf('=')
						select KeyValuePair.Create(value.Remove(index), value.Substring( index+ 1))
					}
				};

				new Rewriter().Rewrite(options);
			});

			return app;
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
