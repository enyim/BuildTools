using System;
using Target;

namespace TestHost
{
	class Program
	{
		static void Main(string[] args)
		{
			LogTests.Test(args);
			new CombinedTests().LogFromTryCatch();
			new CombinedTests().AsyncLogWithError().Wait();
		}
	}
}
