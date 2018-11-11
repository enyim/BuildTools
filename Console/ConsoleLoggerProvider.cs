using System;
using System.Collections.Generic;
using System.Linq;

namespace Enyim.Build
{
	internal sealed class ConsoleLoggerProvider : ILoggerProvider
	{
		public static void Install() => LogManager.Assign(new ConsoleLoggerProvider());

		private ConsoleLoggerProvider() { }
		ILog ILoggerProvider.GetLogger(string name) => new Logger(name);

		private class Logger : ILog
		{
			private enum Severity { Trace, Info, Warn, Error }

			private readonly string name;

			public Logger(string name)
			{
				this.name = name;
			}

			private readonly ConsoleColor[] Colors = new[] { ConsoleColor.Gray, ConsoleColor.White, ConsoleColor.Yellow, ConsoleColor.Red };

			private void Write(Severity severity, string message) => ColoredWriteLine(Colors[(int)severity], $"{DateTime.Now:hh:mm:ss} [{severity.ToString().ToUpperInvariant(),-5}] {name} {message}");

			public void Error(Exception e) => Write(Severity.Error, e.ToString());
			public void Error(string value) => Write(Severity.Error, value);
			public void Info(string value) => Write(Severity.Info, value);
			public void Trace(string value) => Write(Severity.Trace, value);
			public void Warn(string value) => Write(Severity.Warn, value);

			private static void ColoredWriteLine(ConsoleColor fg, string message)
			{
				var tmp = Console.ForegroundColor;

				Console.ForegroundColor = fg;
				Console.WriteLine(message);
				Console.ForegroundColor = tmp;
			}
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
