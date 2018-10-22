using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Enyim.Build.Weavers.LogTo
{
	internal class LoggerImplementer
	{
		private readonly ModuleDefinition module;
		private readonly TypeDefinition typeDef;
		private readonly LogDefinition logDef;

		public Action<string> LogInfo { get; internal set; }

		public LoggerImplementer(LogDefinition logDef, ModuleDefinition module, TypeDefinition typeDef)
		{
			this.module = module;
			this.typeDef = typeDef;
			this.logDef = logDef;
		}

		public bool TryRewrite()
		{
			if (!logDef.IsValid) return false;

			var methods = typeDef.Methods
									.Where(m => m.HasBody && m.Body.Instructions.Any(LogDefinition.IsLogger))
									.ToArray();
			if (methods.Length == 0) return false;

			var logger = logDef.DeclareLogger(typeDef);

			foreach (var method in methods)
			{
				RewriteCalls(method, logger);
			}

			return true;
		}

		private void RewriteCalls(MethodDefinition method, FieldDefinition logger)
		{
			var callCollector = new CallCollector(module);
			var calls = callCollector.Collect(method, LogDefinition.IsLogger);
			var collapsed = calls.SplitToSequences(new SameMethodComparer()).ToList();

			if (collapsed.Count > 0)
			{
				LogInfo($"Rewriting {method.FullName}");

				using (var builder = new BodyBuilder(method))
				{
					foreach (var callGroup in collapsed)
					{
						var start = callGroup.First().StartsAt;
						var label = builder.DefineLabel();
						var end = callGroup.Last().Call;

						builder.InsertBefore(start, Instruction.Create(OpCodes.Ldsfld, logger));
						builder.InsertBefore(start, Instruction.Create(OpCodes.Callvirt, logDef.FindGuard(end)));
						builder.InsertBefore(start, Instruction.Create(OpCodes.Brfalse, label));

						var logMethod = logDef.MapToILog(end);

						foreach (var call in callGroup)
						{
							builder.InsertBefore(call.StartsAt, Instruction.Create(OpCodes.Ldsfld, logger));

							call.Call.OpCode = OpCodes.Callvirt;
							call.Call.Operand = logMethod;
						}

						builder.InsertAfter(end, label);
					}
				}
			}
		}

		private class SameMethodComparer : CallSequenceComparer
		{
			protected override bool IsConsecutive(Instruction left, Instruction right)
			{
				if (left.OpCode.FlowControl == FlowControl.Call && right.OpCode.FlowControl == FlowControl.Call)
					return ((MemberReference)left.Operand).FullName == ((MemberReference)right.Operand).FullName;

				return false;
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
