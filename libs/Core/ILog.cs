using System;
using System.Collections.Generic;
using System.Linq;

namespace Enyim.Build
{
	public interface ILog
	{
		void Trace(string value);
		void Info(string value);
		void Warn(string value);
		void Error(string value);
		void Error(Exception e);
	}
}
