using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Weavers.EventSource
{
	[Order(200)]
	internal class FixStaticCalls : IProcessEventSources
	{
		public void Rewrite(ModuleDefinition module, IEnumerable<ImplementedEventSource> loggers)
		{
			var staticLoggers = loggers.OfType<StaticBasedEventSource>().ToArray();
			if (staticLoggers.Length == 0) return;

			var logMap = staticLoggers.SelectMany(l => l.Methods).ToDictionary(m => m.Old.FullName, m => m.New);
			var instanceMap = staticLoggers.ToDictionary(eventSource => eventSource.Old.FullName, eventSource => (FieldDefinition)eventSource.Meta["Instance"]);
			var isEnabled = staticLoggers.ToDictionary(eventSource => eventSource.Old.FullName, eventSource => module.ImportReference(eventSource.New.FindMethod("IsEnabled")));
			var comparer = new DeclaringTypeComparer();
			var callCollector = new Lazy<CallCollector>(() => new CallCollector(module));

			foreach (var method in WeaverHelpers.AllMethodsWithBody(module))
			{
				FieldDefinition instanceField = null;
				var tmp = from op in method.GetOpsOf(OpCodes.Callvirt, OpCodes.Call)
						  let cls = ((MemberReference)op.Operand).DeclaringType.FullName
						  where instanceMap.TryGetValue(cls, out instanceField)
						  select new { cls, instanceField };

				var instanceFields = tmp.Distinct().ToDictionary(o => o.cls, o => o.instanceField);
				if (instanceFields.Count > 0)
				{
					var calls = callCollector.Value.Collect(method, r => instanceMap.ContainsKey(r.Method.DeclaringType.FullName));
					var groups = calls.SplitToSequences(comparer);

					using (var builder = new BodyBuilder(method.Body))
					{
						foreach (var group in groups)
						{
							var firstCall = group.First();

							var declaringTypeName = firstCall.Call.OperandAsMethod().DeclaringType.FullName;
							var instance = instanceFields[declaringTypeName];

							var label = builder.DefineLabel();

							var start = firstCall.StartsAt;
							builder.InsertBefore(start, Instruction.Create(OpCodes.Ldsfld, instance));
							builder.InsertBefore(start, Instruction.Create(OpCodes.Callvirt, isEnabled[declaringTypeName]));
							builder.InsertBefore(start, Instruction.Create(OpCodes.Brfalse, label));

							builder.InsertAfter(group.Last().Call, label);

							foreach (var call in group)
							{
								builder.InsertBefore(call.StartsAt, Instruction.Create(OpCodes.Ldsfld, instance));

								call.Call.OpCode = OpCodes.Callvirt;
								call.Call.Operand = logMap[call.Call.OperandAsMethod().FullName];
							}
						}
					}
				}
			}
		}

		private class DeclaringTypeComparer : CallSequenceComparer
		{
			protected override bool IsConsecutive(Instruction left, Instruction right)
			{
				Debug.Assert(left.OpCode == OpCodes.Call || left.OpCode == OpCodes.Callvirt);
				Debug.Assert(right.OpCode == OpCodes.Call || right.OpCode == OpCodes.Callvirt);

				return left.OpCode == right.OpCode
						&& ((MemberReference)left.Operand).DeclaringType.FullName == ((MemberReference)right.Operand).DeclaringType.FullName;
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
