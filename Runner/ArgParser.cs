using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Enyim.Build;
using NDesk.Options;

namespace Runner
{
	internal class Options
	{
		private bool help;

		public Options() => Properties = new List<KeyValuePair<string, string>>();

		public FileInfo Weaver { get; private set; }
		public FileInfo Source { get; private set; }
		public FileInfo Target { get; private set; }
		public FileInfo KeyFile { get; private set; }
		public List<KeyValuePair<string, string>> Properties { get; }

		public bool Parse(string[] args)
		{
			try
			{
				var set = CreateOptionSet();
				var extra = set.Parse(args);

				if (help)
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
				{ "w|weaver=", "The path to the weaver assembly.", s => Weaver = new FileInfo(s) },
				{ "p=", "A configuration property in the form of 'name=value'.", s =>
					{
						if (s!= null)
						{
							var idx = s.IndexOf('=');
							if (idx < 0) throw new InvalidOperationException("Invalid property: " + s);

							Properties.Add(new KeyValuePair<string, string>(s.Remove(idx), s.Substring(idx + 1)));
						}
					}
				},
				{ "s|source=", "The path source assembly.", s => Source = new FileInfo(s) },
				{ "o|output=", "The path to the output assembly.", s => Target = new FileInfo(s) },
				{ "key=", "The path of signining key to be used", s => KeyFile = new FileInfo(s) },
				{ "h|help", "Shows the help.", s => help = s != null },
			};

		private void Validate()
		{
			Required("source", Source);
			MustExist("source", Source);

			Required("weaver", Weaver);
			MustExist("weaver", Weaver);

			MustExist("key", KeyFile);
		}

		private void Required<T>(string arg, T value)
		{
			if (value.Equals(default(T)))
				throw new InvalidOperationException("Missing argument: " + arg);
		}

		private void MustExist(string arg, FileInfo file)
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
