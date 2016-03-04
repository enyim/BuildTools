using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Weavers.EventSource
{
	[Order(100)]
	internal class RewriteEventSourceCalls : IProcessEventSources
	{
		public void Rewrite(ModuleDefinition module, IEnumerable<ImplementedEventSource> loggers)
		{
			var implMap = loggers.OfType<InterfaceBasedEventSource>()
								.SelectMany(ies => ies.Methods)
								.ToDictionary(m => m.Old.FullName, m => m.New);
			if (implMap.Count == 0) return;

			foreach (var method in WeaverHelpers.AllMethodsWithBody(module))
			{
				foreach (var op in method.GetOpsOf(OpCodes.Callvirt, OpCodes.Call))
				{
					var target = op.Operand as MethodReference;

					if (target != null)
					{
						MethodDefinition methodDefinition;
						if (implMap.TryGetValue(target.FullName, out methodDefinition))
						{
							op.OpCode = OpCodes.Callvirt;
							op.Operand = methodDefinition;
						}
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
