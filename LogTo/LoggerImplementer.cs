using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Enyim.Build.Rewriters.LogTo
{
	internal class LoggerImplementer : ModuleVisitorBase
	{
		private static readonly ILog log = LogManager.GetLogger<LoggerImplementer>();

		private CallCollector callCollector;
		private LoggerDefinitionFactory logDefinitionFactory;

		public override void BeforeModule(ModuleDefinition module)
		{
			callCollector = new CallCollector(module);
			logDefinitionFactory = new LoggerDefinitionFactory(module);
		}

		public override MethodDefinition BeforeMethod(MethodDefinition method)
		{
			if (!method.HasBody) return method;

			TryDeclareExceptionsForLoggers(method);
			var calls = callCollector.Collect(method, logDefinitionFactory.IsLogger);
			if (calls.Length == 0) return method;

			var collapsed = calls.SplitToSequences(new SameMethodComparer()).ToList();
			if (collapsed.Count == 0) return method;

			log.Info($"Rewriting {method.FullName}");

			using (var builder = new BodyBuilder(method))
			{
				foreach (var callGroup in collapsed)
				{
					var theCall = callGroup.First();
					if (!logDefinitionFactory.TryGet(theCall.Call.TargetMethod().DeclaringType.Resolve(), out var info))
						throw new InvalidOperationException("Log info should have been cached");

					var start = theCall.StartsAt; // where the arguments start
					var label = builder.DefineLabel(); // jump target
					var end = callGroup.Last().Call; // the last call/callvirt in the sequence
					var field = info.DeclareLoggerIn(method.DeclaringType); // the static field containing the logger instance
					var guard = info.TryFindGuard(end); // the IsXXXEnabled method  (optional)

					if (guard == null)
					{
						log.Warn($"There is no IsXXXEnabled defined for {end.TargetMethod()}, no check will be emitted");
					}
					else
					{
						builder.InsertBefore(start, Instruction.Create(OpCodes.Ldsfld, field));
						builder.InsertBefore(start, Instruction.Create(OpCodes.Callvirt, guard));
						builder.InsertBefore(start, Instruction.Create(OpCodes.Brfalse, label));
					}

					var logMethod = info.MapToILog(end);

					foreach (var call in callGroup)
					{
						builder.InsertBefore(call.StartsAt, Instruction.Create(OpCodes.Ldsfld, field));

						call.Call.OpCode = OpCodes.Callvirt;
						call.Call.Operand = logMethod;
					}

					if (guard != null)
					{
						builder.InsertAfter(end, label);
					}
				}
			}

			return method;
		}

		private class SameMethodComparer : CallSequenceComparer
		{
			protected override bool IsConsecutive(Instruction left, Instruction right)
			{
				if (left.Is(OpCodes.Call, OpCodes.Callvirt) && right.Is(OpCodes.Call, OpCodes.Callvirt))
					return left.TargetMethod().FullName == right.TargetMethod().FullName;

				return false;
			}
		}

		private void TryDeclareExceptionsForLoggers(MethodDefinition method)
		{
			return;

			if (!method.Body.HasExceptionHandlers) return;

			// in release builds if the LogTo.Error (or similar) immediately follows the catch(E) block
			// the compiler will not declare a local variable for the exception (as the catch block already has it on stack)
			// which messes up the call analyzer
			// in these cases, we introduce a local variable for the exception
			var calls = method.GetOpsOf(OpCodes.Call, OpCodes.Callvirt).Where(logDefinitionFactory.IsLogger).ToArray();

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
					var ldloc = Instruction.Create(OpCodes.Ldloc, local);

					ilp.InsertBefore(eh.HandlerStart, stloc);
					ilp.InsertBefore(eh.HandlerStart, ldloc);

					eh.HandlerStart = stloc;
					eh.TryEnd = stloc;
				}

				//method.Body.Optimize();
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
