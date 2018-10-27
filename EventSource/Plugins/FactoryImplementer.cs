using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.EventSource
{
	// replaces the EventSourceFactory.Get<TEventSource>() calls with 'new TImplementedEventSource()'
	internal class FactoryImplementer : EventSourceRewriter
	{
		private static readonly ILog log = LogManager.GetLogger<FactoryImplementer>();

		private TypeReference factory;
		private Dictionary<string, ImplementedEventSource> implMap;
		private string fullNameOfGet;
		private Dictionary<string, TypeDefinition> rewrittenTypeReferences;

		public FactoryImplementer(IEnumerable<ImplementedEventSource> implementations) : base(implementations) { }

		public override void BeforeModule(ModuleDefinition module)
		{
			Enabled = false;

			// EventSourceFactory must be an empty class in the target assembly
			factory = module.IncludeReferencedTypes().Named("EventSourceFactory");
			if (factory == null) return;

			// check if any loggers have been implemented
			implMap = Implementations.ToDictionary(l => l.Old.FullName);
			if (implMap.Count == 0) return;

			// get the TEventSource from the Get<TEventSource>() method
			fullNameOfGet = module.ImportReference(factory).Resolve().FindMethod("Get").ToString();
			rewrittenTypeReferences = new Dictionary<string, TypeDefinition>();

			Enabled = true;
		}

		public override TypeDefinition BeforeType(TypeDefinition type) => CilComparer.AreSame(type, factory) ? null : type;

		public override Instruction MethodInstruction(MethodDefinition owner, Instruction instruction)
		{
			Debug.Assert(Enabled, "Is Enabled");

			if (instruction.OpCode == OpCodes.Call)
			{
				var mr = instruction.TargetMethod();
				if (mr.IsGenericInstance && mr.GetElementMethod().ToString() == fullNameOfGet)
				{
					var requestedType = ((GenericInstanceMethod)mr).GenericArguments[0].Resolve();

					// if the factory creates an interface, resolve it to the implemented type
					// otherwise use the type argument from the generic method
					var target = requestedType.IsClass
									? requestedType.Resolve()
									: implMap.TryGetValue(requestedType.FullName, out var ie)
										? ie.New
										: null;
					if (target != null)
					{
						var ctor = target.FindConstructor();
						if (ctor == null)
							throw new InvalidOperationException($"{target.FullName} has no constructor");

						rewrittenTypeReferences[requestedType.FullName] = target;
						log.Info($"Factory: {requestedType} -> {target}");

						return Instruction.Create(OpCodes.Newobj, ctor);
					}
					else
					{
						log.Warn($"Factory: cannot rewrite {requestedType}, no target type was found");
					}
				}
			}

			return instruction;
		}

		public override void AfterMethod(MethodDefinition method)
		{
			// fix the local variables so they point to the new implementation
			if (method.HasBody)
			{
				foreach (var v in method.Body.Variables)
				{
					Debug.Assert(rewrittenTypeReferences !=null, "1");
					Debug.Assert(v != null, "2");
					Debug.Assert(v.VariableType != null, "3");
					Debug.Assert(v.VariableType.FullName != null, "4");

					if (rewrittenTypeReferences.TryGetValue(v.VariableType.FullName, out var target))
					{
						v.VariableType = target;
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
