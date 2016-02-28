using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Target
{
	public class LogTests
	{
		static void Main(string[] args)
		{
			LogTo.Debug("1");

			LogTo.Info("1", 2);

			LogTo.Debug("1");
			LogTo.Debug("1");
		}
	}

	static class LogTo
	{
		public static void Debug(string a) { }
		public static void Info(string a, int b) { }
	}

	interface ILog
	{
		void Debug(string a);
		void Info(string a, int b);

		bool IsDebugEnabled { get; }
		bool IsInfoEnabled { get; }
	}

	static class LogManager
	{
		public static ILog GetCurrentClassLogger()
		{
			return null;
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
