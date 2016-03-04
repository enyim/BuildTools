using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace Enyim.Build.Weavers.EventSource
{
	[Order(-10000)]
	internal class OptimizeImplementedMethods : IProcessEventSources
	{
		public void Rewrite(ModuleDefinition module, IEnumerable<ImplementedEventSource> loggers)
		{
			foreach (var m in loggers.SelectMany(l => l.New.Methods))
			{
				m.Body.OptimizeMacros();
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
