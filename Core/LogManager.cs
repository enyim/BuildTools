using System;
using System.Collections.Generic;
using System.Linq;

namespace Enyim.Build
{
	public static class LogManager
	{
		private static ILoggerProvider instance = new NullLoggerProvider();
		public static void Assign(ILoggerProvider instance) => LogManager.instance = instance;

		public static ILog GetLogger<T>() => instance.GetLogger(typeof(T).FullName);
		public static ILog GetLogger(string name) => instance.GetLogger(name);

		private class NullLoggerProvider : ILoggerProvider
		{
			private static readonly NullLogger instance = new NullLogger();
			public ILog GetLogger(string name) => instance;
		}

		private class NullLogger : ILog
		{
			public void Error(string value) { }
			public void Error(Exception e) { }
			public void Info(string value) { }
			public void Trace(string value) { }
			public void Warn(string value) { }
		}
	}
}
