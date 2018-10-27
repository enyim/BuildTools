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
		private Dictionary<string, MethodReference> isEnabledMap;
		private Lazy<CallCollector> callCollector;

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
			isEnabledMap = staticTracers.ToDictionary(eventSource => eventSource.Old.FullName, eventSource => module.ImportReference(eventSource.New.FindMethod("IsEnabled")));

			callCollector = new Lazy<CallCollector>(() => new CallCollector(module));
		}

		public override MethodDefinition BeforeMethod(MethodDefinition method)
		{
			var calls = callCollector.Value.Collect(method, r => instanceMap.ContainsKey(r.TargetMethod().DeclaringType.FullName));
			if (calls.Length == 0) return method;

#if DO_GUARDS
			var groups = calls.SplitToSequences(comparer);
#endif

			using (var builder = new BodyBuilder(method.Body))
			{
#if DO_GUARDS
				foreach (var group in groups)
				{
					var firstCall = group.First();

					var declaringTypeName = firstCall.Call.TargetMethod().DeclaringType.FullName;
					var instance = instanceMap[declaringTypeName];

					var label = builder.DefineLabel();

					var start = firstCall.StartsAt;
					builder.InsertBefore(start, Instruction.Create(OpCodes.Ldsfld, instance));
					builder.InsertBefore(start, Instruction.Create(OpCodes.Callvirt, isEnabledMap[declaringTypeName]));
					builder.InsertBefore(start, Instruction.Create(OpCodes.Brfalse, label));

					builder.InsertAfter(group.Last().Call, label);

					foreach (var call in group)
					{
						builder.InsertBefore(call.StartsAt, Instruction.Create(OpCodes.Ldsfld, instance));

						call.Call.OpCode = OpCodes.Callvirt;
						call.Call.Operand = logMap[call.Call.TargetMethod().FullName];
					}
				}
#else
				foreach (var call in calls)
				{
					var declaringTypeName = call.Call.TargetMethod().DeclaringType.FullName;
					var instance = instanceMap[declaringTypeName];

					builder.InsertBefore(call.StartsAt, Instruction.Create(OpCodes.Ldsfld, instance));

					call.Call.OpCode = OpCodes.Callvirt;
					call.Call.Operand = logMap[call.Call.TargetMethod().FullName];
				}
#endif
			}

			return method;
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
