using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Weavers.EventSource
{
	[Order(int.MinValue)]
	internal class FactoryImplementer : IProcessEventSources
	{
		public void Rewrite(ModuleDefinition module, IEnumerable<ImplementedEventSource> loggers)
		{
			var factory = module.IncludeReferencedTypes().FirstOrDefault(t => t.Name == "EventSourceFactory");
			if (factory == null) return;

			var implMap = loggers.ToDictionary(l => l.Old.FullName);
			if (implMap.Count == 0) return;

			var fullNameOfGet = module.ImportReference(factory).Resolve().FindMethod("Get").FullName;
			var methods = WeaverHelpers.AllMethodsWithBody(module).ToArray();
			var mapped = new Dictionary<string, TypeDefinition>();

			foreach (var method in methods)
			{
				var calls = (from i in method.GetOpsOf(OpCodes.Call)
							 let mr = i.Operand as MethodReference
							 where mr.IsGenericInstance
								   && mr.Resolve().GetElementMethod().FullName == fullNameOfGet
							 select new
							 {
								 Instruction = i,
								 Wanted = ((GenericInstanceMethod)mr).GenericArguments[0].Resolve()
							 }).ToArray();

				if (calls.Length != 0)
				{
					var ilp = method.Body.GetILProcessor();
					foreach (var call in calls)
					{
						// if the factory creates an interface, resolve it to the implemenetd type
						// otherwise use the type argument from the generic method
						var target = call.Wanted.IsClass
										? call.Wanted.Resolve()
										: implMap.TryGetValue(call.Wanted.FullName, out var ie)
											? ie.New
											: null;
						if (target == null)
						{
							Log.Warn($"Factory: cannot rewrite {call.Wanted.FullName}");
							continue;
						}

						var ctor = target.FindConstructor();
						if (ctor == null)
							throw new InvalidOperationException($"{target.FullName} has no constructor");

						var @new = Instruction.Create(OpCodes.Newobj, ctor);
						//@new.SequencePoint = method.DebugInformation.GetSequencePoint(call.Instruction);
						ilp.Replace(call.Instruction, @new);

						Log.Info($"Factory: {call.Wanted.FullName} -> {target.FullName}");
						mapped[call.Wanted.FullName] = target;
					}
				}

				RewriteLocalVariables(method, mapped);
			}
		}

		private static void RewriteLocalVariables(MethodDefinition method, IReadOnlyDictionary<string, TypeDefinition> mapped)
		{
			foreach (var v in method.Body.Variables)
			{
				if (mapped.TryGetValue(v.VariableType.FullName, out var target))
				{
					v.VariableType = target;
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
