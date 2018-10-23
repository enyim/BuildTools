using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enyim.Build;
using NDesk.Options;

namespace Enyim.Build
{
	public enum DebugSymbolsKind { None, Embedded, Portable };

	internal class Options
	{
		private bool showHelp;

		public FileInfo Rewriter { get; private set; }
		public FileInfo Source { get; private set; }
		public FileInfo Target { get; private set; }

		public bool SignAssembly { get; private set; }
		public FileInfo KeyFile { get; private set; }

		public DebugSymbolsKind Symbols { get; private set; }

		public List<KeyValuePair<string, string>> Properties { get; } = new List<KeyValuePair<string, string>>();

		public bool Parse(string[] args)
		{
			try
			{
				var set = CreateOptionSet();
				var extra = set.Parse(args);

				if (showHelp)
				{
					Console.Write("Usage: runner OPTIONS");
					set.WriteOptionDescriptions(Console.Out);
					return false;
				}
				else if (extra.Count > 0)
				{
					throw new InvalidOperationException("Unknown argument: " + extra[0]);
				}

				Validate();

				return true;
			}
			catch (Exception e)
			{
				Console.Write("runner: ");
				Console.WriteLine(e.Message);
				Console.WriteLine("Try `runner --help' for more information.");

				return false;
			}
		}

		private OptionSet CreateOptionSet()
			=> new OptionSet
			{
				{ "r|rewriter=", "The path to the rewriter plugin's assembly.", value => Rewriter = new FileInfo(value) },
				{ "s|source=", "The path source assembly.", value => Source = new FileInfo(value) },
				{ "o|output=", "The path to the output assembly.", value => Target = new FileInfo(value) },
				{ "sign+", "If the assembly should be signed", value => SignAssembly = !String.IsNullOrWhiteSpace(value) },
				{ "key=", "The path of signining key to be used", value => KeyFile = new FileInfo(value) },
				{ "p=", "A configuration property in the form of 'name=value'.", value =>
					{
						if (value!= null)
						{
							var idx = value.IndexOf('=');
							if (idx < 0) throw new InvalidOperationException("Invalid property: " + value);

							Properties.Add(new KeyValuePair<string, string>(value.Remove(idx), value.Substring(idx + 1)));
						}
					}
				},
				{ "?|h|help", "Shows the help.", value => showHelp = !String.IsNullOrWhiteSpace(value) },
			}
			.AddEnumSwitch<DebugSymbolsKind>("symbols=", "If debug symbols should be emitted and in what format.", value => Symbols = value);

		private void Validate()
		{
			Required("source", Source);
			MustExistIfSet("source", Source);

			Required("rewriter", Rewriter);
			MustExistIfSet("rewriter", Rewriter);

			MustExistIfSet("key", KeyFile);
		}

		private void Required<T>(string arg, T value)
		{
			if (value.Equals(default))
				throw new InvalidOperationException("Missing argument: " + arg);
		}

		private void MustExistIfSet(string arg, FileInfo file)
		{
			if (file?.Exists == false)
				throw new InvalidOperationException($"Invalid argument {arg}: File not found.");
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
