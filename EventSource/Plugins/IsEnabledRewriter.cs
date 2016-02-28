using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class IsEnabledRewriter : IProcessEventSources
	{
		public void Rewrite(ModuleDefinition module, IEnumerable<ImplementedEventSource> loggers)
		{
			var source = from l in loggers.OfType<InterfaceBasedEventSource>()
						 let old = l.Old.FindMethod("IsEnabled")
						 where old != null
						 select new
						 {
							 Old = old,
							 New = l.New.BaseType.Resolve().FindMethod("IsEnabled").ImportInto(module)
						 };

			var fixMap = Enumerable.ToDictionary(source, a => a.Old.FullName, a => a.New);
			if (fixMap.Count == 0) return;

			var allMethods = module.Types.SelectMany(t => t.Methods).Where(m => m.HasBody).ToArray();

			foreach (var method in allMethods)
			{
				foreach (var instruction in method.GetOpsOf(OpCodes.Callvirt).ToArray())
				{
					MethodReference impl;
					var target = instruction.Operand as MethodReference;

					if (target != null && fixMap.TryGetValue(target.FullName, out impl))
					{
						instruction.OpCode = OpCodes.Call;
						instruction.Operand = impl;
					}
				}
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
