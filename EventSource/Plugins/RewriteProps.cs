﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class RewriteProps : IProcessEventSources
	{
		public void Rewrite(ModuleDefinition module, IEnumerable<ImplementedEventSource> loggers)
		{
			var implMap = loggers.OfType<InterfaceBasedEventSource>().ToDictionary(l => l.Old.FullName);
			if (implMap.Count == 0) return;

			foreach (var p in module.Types.IncludeNestedTypes().SelectMany(t => t.Properties))
			{
				InterfaceBasedEventSource target;

				if (implMap.TryGetValue(p.PropertyType.FullName, out target))
					p.PropertyType = target.New;
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