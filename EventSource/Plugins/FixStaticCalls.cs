using System;
using System.Collections.Generic;
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
			var instanceMap = staticLoggers.ToDictionary(s => s.Old.FullName, s => (FieldDefinition)s.Meta["Instance"]);
			var isEnabled = staticLoggers.ToDictionary(s => s.Old.FullName, s => module.Import(CecilExtensions.FindMethod(s.New, "IsEnabled")));
			var comparer = new DeclaringTypeComparer();

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
					var cc = new CallCollector(module);
					var calls = cc.FindCalls(method, r => instanceMap.ContainsKey(r.DeclaringType.FullName));
					var groups = cc.CollapseBlocks(calls, comparer);

					using (var builder = new BodyBuilder(method.Body))
					{
						foreach (var group in groups)
						{
							var declaringTypeName = group.Calls.First().Method.DeclaringType.FullName;
							var instance = instanceFields[declaringTypeName];

							var label = builder.DefineLabel();

							builder.InsertBefore(group.Start, Instruction.Create(OpCodes.Ldsfld, instance));
							builder.InsertBefore(group.Start, Instruction.Create(OpCodes.Callvirt, isEnabled[declaringTypeName]));
							builder.InsertBefore(group.Start, Instruction.Create(OpCodes.Brfalse, label));

							builder.InsertAfter(group.End, label);

							foreach (var call in group.Calls)
							{
								builder.InsertBefore(call.Start, Instruction.Create(OpCodes.Ldsfld, instance));

								call.End.OpCode = OpCodes.Callvirt;
								call.End.Operand = logMap[call.Method.FullName];
							}
						}
					}
				}
			}
		}

		private class DeclaringTypeComparer : IEqualityComparer<Instruction>
		{
			public bool Equals(Instruction x, Instruction y)
			{
				return x.OpCode == y.OpCode
						&& (x.OpCode == OpCodes.Call || x.OpCode == OpCodes.Callvirt)
						&& ((MemberReference)x.Operand).DeclaringType.FullName == ((MemberReference)y.Operand).DeclaringType.FullName;
			}

			public int GetHashCode(Instruction obj)
			{
				return obj.ToString().GetHashCode();
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
