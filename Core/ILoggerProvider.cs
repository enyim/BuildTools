using System;
using System.Collections.Generic;
using System.Linq;

namespace Enyim.Build
{
	public interface ILoggerProvider
	{
		ILog GetLogger(string name);
	}
}
