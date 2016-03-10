﻿using System;
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
			if (!this.logDef.IsValid) return false;

			var methods = GetMethods().ToArray();
			if (methods.Length == 0) return false;

			var logger = logDef.DeclareLogger(typeDef);

			foreach (var method in methods)
			{
				DeclareExceptionsForLoggers(method);
				RewriteCalls(method, logger);
			}

			return true;
		}

		private IEnumerable<MethodDefinition> GetMethods()
		{
			return typeDef.Methods.Where(m => m.HasBody && m.Body.Instructions.Any(LogDefinition.IsLogger));
		}

		private void DeclareExceptionsForLoggers(MethodDefinition method)
		{
			if (!method.Body.HasExceptionHandlers) return;

			// in release builds if the LogTo.Error (or similar) immediately follows the catch(E) block
			// the compiler will not declare a local variable for the exception (as the catch block already has it on stack)
			// which messes up the call analyzer
			// in these cases, we introduce a local variable for the exception
			var calls = method.GetOpsOf(OpCodes.Call, OpCodes.Callvirt).Where(LogDefinition.IsLogger).ToArray();

			if (calls.Length > 0)
			{
				var ilp = method.Body.GetILProcessor();

				var ehToFix = method.Body
									.ExceptionHandlers
									.Where(eh => eh.HandlerType == ExceptionHandlerType.Catch
													&& calls.Any(c => eh.HandlerStart.Offset <= c.Offset
																		&& c.Offset <= eh.HandlerEnd.Offset))
									.ToArray();

				foreach (var eh in ehToFix)
				{
					var local = new VariableDefinition(eh.CatchType);
					method.Body.Variables.Add(local);

					var stloc = Instruction.Create(OpCodes.Stloc, local);
					ilp.InsertBefore(eh.HandlerStart, stloc);
					ilp.InsertBefore(eh.HandlerStart, Instruction.Create(OpCodes.Ldloc, local));
					eh.HandlerStart = stloc;
					eh.TryEnd = stloc;
				}

				method.Body.OptimizeMacros();
			}
		}

		private void RewriteCalls(MethodDefinition method, FieldDefinition logger)
		{
			var callCollector = new CallCollector(this.module);
			var calls = callCollector.FindCalls(method, LogDefinition.IsLogger);
			var collapsed = callCollector.CollapseBlocks(calls, new SameMethodComparer());

			if (collapsed.Length > 0)
			{
				LogInfo($"Rewriting {method.FullName}");

				using (var builder = new BodyBuilder(method))
				{
					foreach (var callGroup in collapsed)
					{
						var start = callGroup.Start;
						var label = builder.DefineLabel();
						var end = callGroup.Calls.First().End;

						builder.InsertBefore(start, Instruction.Create(OpCodes.Ldsfld, logger));
						builder.InsertBefore(start, Instruction.Create(OpCodes.Callvirt, this.logDef.FindGuard(end)));
						builder.InsertBefore(start, Instruction.Create(OpCodes.Brfalse, label));

						var logMethod = logDef.MapToILog(end);

						foreach (var call in callGroup.Calls)
						{
							builder.InsertBefore(call.Start, Instruction.Create(OpCodes.Ldsfld, logger));

							call.End.OpCode = OpCodes.Callvirt;
							call.End.Operand = logMethod;
						}

						builder.InsertAfter(callGroup.End, label);
					}
				}
			}
		}

		private class SameMethodComparer : IEqualityComparer<Instruction>
		{
			public bool Equals(Instruction x, Instruction y)
			{
				if (x.OpCode.FlowControl == FlowControl.Call && y.OpCode.FlowControl == FlowControl.Call)
					return ((MemberReference)x.Operand).FullName == ((MemberReference)y.Operand).FullName;

				return false;
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