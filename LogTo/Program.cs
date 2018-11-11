#define EASY_DEBUG
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Enyim.Build
{
	internal class Program: ProgramBase
	{
		public static void Main(string[] args)
		{
#if EASY_DEBUG
			if (Debugger.IsAttached)
			{
				string Source;

				Source = @"D:\Repo\BuildTools\Tests\TargetNetstd\bin\Debug\netstandard2.0\TargetNetstd.dll";
				//string Source = @"D:\Repo\BuildTools\Tests\TestReferences\bin\Debug\netstandard2.0\TestReferences.dll";
				//string Source = @"D:\Repo\BuildTools\Tests\TestReferences\obj\Debug\netstandard2.0\TestReferences.dll";
				Source = @"D:\Repo\enyimmemcached2\Memcached\bin\debug\netstandard2.0\Enyim.Caching.Memcached2.dll";

				const string Output = "--output d:\\lofasz.dll";

				args = $@"{Source} {Output} --debugsymbols:true --debugtype portable".Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
				//args = $"--source {Source} --rewriter logto -w pina -r ref1.dll --reference ref2.dll;ref3.dll -p a=1;b=2;c=3 --property d=4 --debugsymbols:true ".Split(' ', options: StringSplitOptions.RemoveEmptyEntries);
			}
#endif

			Run(args, new Rewriters.LogTo.ModuleRewriter());
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
