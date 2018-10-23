using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.EventSource
{
	// if the interface template has IsEnabled() declared rewrite the call to EventSource.IsEnabled()
	internal class IsEnabledRewriter : EventSourceRewriter
	{
		private Dictionary<string, MethodReference> fixMap;

		public IsEnabledRewriter(IEnumerable<ImplementedEventSource> implementations) : base(implementations) { }

		public override void BeforeModule(ModuleDefinition module)
		{
			var source = from impl in Implementations.OfType<InterfaceBasedEventSource>()
						 let old = impl.Old.FindMethod("IsEnabled")
						 where old != null
						 select new
						 {
							 Old = old,
							 New = impl.New.BaseType.Resolve().FindMethod("IsEnabled").ImportInto(module)
						 };

			fixMap = source.ToDictionary(a => a.Old.ToString(), a => a.New);
		}

		public override Instruction MethodInstruction(MethodDefinition owner, Instruction instruction)
		{
			if (instruction.OpCode == OpCodes.Callvirt
				&& fixMap.TryGetValue(instruction.TargetMethod().ToString(), out var impl))
			{
				instruction.OpCode = OpCodes.Call;
				instruction.Operand = impl;
			}

			return instruction;
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
