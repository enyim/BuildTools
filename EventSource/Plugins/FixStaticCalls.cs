using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.EventSource
{
	internal class FixStaticCalls : EventSourceRewriter
	{
#if DO_GUARDS
		private static readonly DeclaringTypeComparer comparer = new DeclaringTypeComparer();
#endif

		private Dictionary<string, MethodDefinition> logMap;
		private Dictionary<string, FieldDefinition> instanceMap;

		public FixStaticCalls(IEnumerable<ImplementedEventSource> implementations) : base(implementations) { }

		public override void BeforeModule(ModuleDefinition module)
		{
			var staticTracers = Implementations.OfType<StaticBasedEventSource>().ToArray();
			if (staticTracers.Length == 0)
			{
				Enabled = false;
				return;
			}

			logMap = staticTracers.SelectMany(l => l.Methods).ToDictionary(m => m.Old.FullName, m => m.New);
			instanceMap = staticTracers.ToDictionary(eventSource => eventSource.Old.FullName, eventSource => (FieldDefinition)eventSource.Meta["Instance"]);
		}

		public override MethodDefinition BeforeMethod(MethodDefinition method)
		{
			if (!method.HasBody) return method;

			var calls = new List<(Instruction instruction, FieldDefinition instanceField)>();

			foreach (var i in method.Body.Instructions)
			{
				if (i.OpCode != OpCodes.Call) continue;

				var key = i.TargetMethod().DeclaringType.FullName;
				if (instanceMap.TryGetValue(key, out var field))
				{
					calls.Add((i, field));
				}
			}

			if (calls.Count == 0) return method;

			using (var builder = new BodyBuilder(method.Body))
			{
				foreach (var call in calls)
				{
					var instruction = call.instruction;
					builder.InsertBefore(instruction, Instruction.Create(OpCodes.Ldsfld, call.instanceField));

					instruction.OpCode = OpCodes.Callvirt;
					instruction.Operand = logMap[instruction.TargetMethod().FullName];
				}
			}

			return method;
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
