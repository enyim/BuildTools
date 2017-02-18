using System;
using System.Linq;
using System.Collections.Generic;

namespace Runner
{
	class ColoredConsoleLogger : ILogger
	{
		public void Info(string message)
		{
			Log(ConsoleColor.White, "INFO ", message);
		}

		public void Warn(string message)
		{
			Log(ConsoleColor.Yellow, "WARN ", message);
		}

		public void Error(string message)
		{
			Log(ConsoleColor.Red, "ERROR", message);
		}

		private static void Log(ConsoleColor fg, string level, string message)
		{
			ConsoleHelper.ColoredWriteLine(fg, $"    {level} {message}");
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
