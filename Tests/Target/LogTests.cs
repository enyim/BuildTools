using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Target
{
	public static class LogTests
	{
		public static void Test(string[] args)
		{
			LogTo.Debug("1");

			LogTo.Info("1", 2);

			LogTo.Debug("1");

			LogTo.Debug("1");
		}
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class MapLogToAttribute : System.Attribute
	{
		public Type ILog { get; set; }
		public Type LogManager { get; set; }
	}

	[MapLogTo(ILog = typeof(ILog), LogManager = typeof(LogManager))]
	public static class LogTo
	{
		public static void Debug(string a) { }
		public static void Info(string a, int b) { }
		public static void Error(Exception e) { }
		public static void Warn(Exception e) { }
	}

	[MapLogTo(ILog = typeof(IHellog), LogManager = typeof(HellogManager))]
	public static class HellogTo
	{
		public static void Debug(string a) { }
		public static void Info(string a, int b) { }
		public static void Hello(string a) { }
	}

	public interface IHellog
	{
		void Debug(string a);
		void Info(string a, int b);
		void Hello(string a);

		bool IsDebugEnabled { get; }
		bool IsInfoEnabled { get; }
		bool IsHelloEnabled { get; }
	}

	internal interface ILog
	{
		void Debug(string a);
		void Info(string a, int b);
		void Error(Exception e);
		void Warn(Exception e);

		bool IsDebugEnabled { get; }
		bool IsInfoEnabled { get; }
		bool IsErrorEnabled { get; }
		bool IsWarnEnabled { get; }
	}

	internal static class LogManager
	{
		public static ILog GetLogger(string name) => new ConsoleLogger(name);
		public static ILog GetLogger(Type type) => new ConsoleLogger(type.FullName);

		class ConsoleLogger : ILog
		{
			private readonly string name;

			public ConsoleLogger(string name)
			{
				this.name = name;
			}

			public bool IsDebugEnabled => true;
			public bool IsInfoEnabled => true;
			public bool IsErrorEnabled => true;
			public bool IsWarnEnabled => true;

			public void Debug(string a)
			{
				Console.WriteLine($"DEBUG {name} -- {a}");
			}

			public void Error(Exception e)
			{
				Console.WriteLine($"ERROR {name} -- {e}");
			}

			public void Info(string a, int b)
			{
				Console.WriteLine($"INFO {name} -- {a} {b}");
			}

			public void Warn(Exception e)
			{
				Console.WriteLine($"WARN {name} -- {e}");
			}
		}
	}

	public static class HellogManager
	{
		public static IHellog GetLogger(string name) => new ConsoleLogger(name);
		public static IHellog GetLogger(Type type) => new ConsoleLogger(type.FullName);

		class ConsoleLogger : IHellog
		{
			private readonly string name;

			public ConsoleLogger(string name)
			{
				this.name = name;
			}

			public bool IsDebugEnabled => true;
			public bool IsInfoEnabled => true;
			public bool IsHelloEnabled => true;

			public void Debug(string a)
			{
				Console.WriteLine($"DEBUG {name} -- {a}");
			}

			public void Hello(string a)
			{
				Console.WriteLine($"HELLO {name} -- {a}");
			}

			public void Info(string a, int b)
			{
				Console.WriteLine($"INFO {name} -- {a} {b}");
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
