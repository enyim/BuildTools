#define EASY_DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
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
				string Source;

				Source = @"D:\Repo\BuildTools\Tests\TargetNetstd\bin\Debug\netstandard2.0\TargetNetstd.dll";
				//string Source = @"D:\Repo\BuildTools\Tests\TestReferences\bin\Debug\netstandard2.0\TestReferences.dll";
				//string Source = @"D:\Repo\BuildTools\Tests\TestReferences\obj\Debug\netstandard2.0\TestReferences.dll";
				Source = @"D:\Repo\enyimmemcached2\Memcached\bin\debug\netstandard2.0\Enyim.Caching.Memcached2.dll";

				const string Output = "--output d:\\lofasz.dll";

				args = $@"LogTo {Source} {Output} --debugsymbols:true --debugtype portable".Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
				//args = $"--source {Source} --rewriter logto -w pina -r ref1.dll --reference ref2.dll;ref3.dll -p a=1;b=2;c=3 --property d=4 --debugsymbols:true ".Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
			}
#endif

			ConsoleLoggerProvider.Install();

			Run(args, o => new Rewriter().Rewrite(o));
		}

		public static void Run(string[] args, Action<Options> how)
		{
			var app = new CommandLineApplication
			{
				Name = "runner",
				HelpTextGenerator = new CustomHelpGenerator()
			};

			app.ValueParsers.Add(new FileInfoValueParser());
			app.ValueParsers.Add(new SemicolonListValueParser());

			app.HelpOption();
			app.VersionOptionFromAssemblyAttributes(typeof(Program).Assembly);

			var rewriter = app.Argument<FileInfo>(
								"rewriter",
								"Name of the rewriter to be used.")
								.IsRequired()
								.Accepts(v => v.ExistingFile());

			var source = app.Argument<FileInfo>(
								"source",
								"Location of the source assembly to be processed")
								.IsRequired()
								.Accepts(v => v.ExistingFile());

			var target = app.Option<FileInfo>("-o | --output <path>",
								"The output path where the rewritten assembly will be copied to; " +
								"if not specified the source will be overwritten",
								CommandOptionType.SingleValue);

			var debugType = app.Option<DebugType>(
								"--debugtype <value>",
								$"Type of debug symbols to be emitted. ({String.Join(", ", Enum.GetNames(typeof(DebugType)))})", CommandOptionType.SingleValue)
								.Accepts(v => v.Enum<DebugType>(true));

			var debugSymbols = app.Option<bool>(
								"--debugsymbols <value>",
								$"If debug symbols should be emitted.", CommandOptionType.SingleValue);

			var props = app.Option(
								"-p | --property <value>",
								"Property values for the rewriter in the format of 'name=value'",
								CommandOptionType.MultipleValue)
								.Accepts(v => v.RegularExpression("^([^=]+)=(.+)"));

			var refs = app.Option(
					"-r | --reference <value>",
					"List of extra reference assemblies. Specify the parameter multiple times or separate assemblies by semicolon.",
					CommandOptionType.MultipleValue);

			app.OnExecute(() =>
			{
				var o = new Options
				{
					Rewriter = rewriter.ParsedValue,
					Source = source.ParsedValue,
					Target = target.HasValue() ? target.ParsedValue : null,
					DebugType = debugType.HasValue() ? (DebugType?)debugType.ParsedValue : null,
					DebugSymbols = debugSymbols.HasValue() && debugSymbols.ParsedValue
				};

				AddSemicolonMultiList(o.References, refs);
				AddSemicolonMultiList(o.Properties, props, value =>
				{
					var index = value.IndexOf('=');
					return KeyValuePair.Create(value.Remove(index), value.Substring(index + 1));
				});

				how(o);
			});

			app.Execute(args);
		}

		private static void AddSemicolonMultiList(List<string> target, CommandOption option)
		{
			if (!option.HasValue()) return;

			foreach (var v in option.Values)
				target.AddRange(v.Split(';', StringSplitOptions.RemoveEmptyEntries));
		}

		private static void AddSemicolonMultiList<T>(List<T> target, CommandOption option, Func<string, T> parser)
		{
			if (!option.HasValue()) return;

			foreach (var v in option.Values)
				target.AddRange(v.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(parser));
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
