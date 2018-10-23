using System;
using System.Collections.Generic;
using System.Linq;

namespace Enyim.Build
{
	public static class LogManager
	{
		private static ILoggerProvider instance;
		public static void Assign(ILoggerProvider instance) => LogManager.instance = instance;

		public static ILog GetLogger<T>() => instance.GetLogger(typeof(T).FullName);
		public static ILog GetLogger(string name) => instance.GetLogger(name);
	}
}
