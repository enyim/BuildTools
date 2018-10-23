using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Rewriters.LogTo
{
	using BindingFlags = System.Reflection.BindingFlags;

	public class LoggerDefinition
	{
		private readonly Dictionary<string, MethodReference> guardProperties;
		private readonly Dictionary<string, MethodReference> ilogMap;
		private readonly Dictionary<TypeDefinition, FieldDefinition> Instances = new Dictionary<TypeDefinition, FieldDefinition>(TypeDefinitionComparer.Instance);

		private readonly MethodReference getTypeFromHandle;

		public LoggerDefinition(ModuleDefinition module, TypeDefinition logTo, TypeDefinition ilog, TypeDefinition logManager)
		{
			LogTo = logTo;
			ILog = ilog;
			LogManager = logManager;

			ilogMap = GetILogMap(module, logTo, ilog);
			guardProperties = GetGuardProperties(module, ilog);

			getTypeFromHandle = module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
		}

		public readonly TypeDefinition LogTo; //source
		public readonly TypeDefinition ILog; // target
		public readonly TypeDefinition LogManager; // factory

		public FieldDefinition DeclareLoggerIn(TypeDefinition type)
		{
			if (type.Interfaces.Any(t => t.InterfaceType.Name == "IAsyncStateMachine"))
				type = type.DeclaringType;

			if (!Instances.TryGetValue(type, out var retval))
			{
				retval = type.Fields.FirstOrDefault(f => f.FieldType.FullName == ILog.FullName);
				var module = type.Module;

				if (retval == null)
				{
					retval = type.DeclareStaticField(module, module.ImportReference(ILog), "<>log_" + type.Fields.Count, (field) =>
						new[]
						{
							Instruction.Create(OpCodes.Stsfld, field),
							Instruction.Create(OpCodes.Ldtoken, type),
							Instruction.Create(OpCodes.Call, getTypeFromHandle),
							Instruction.Create(OpCodes.Call, LogManager
																.ImportInto(module).Resolve()
																.FindMethod("GetLogger", new [] { module.ImportReference(typeof(Type)) })
																?? throw new InvalidOperationException($"Cannot find GetLogger(Type) on {LogManager}"))
						}, FieldAttributes.Private);
				}

				Instances.Add(type, retval);
			}

			return retval;
		}

		public MethodReference MapToILog(MethodReference md) => ilogMap[md.ToString()];
		public MethodReference MapToILog(Instruction instruction) => MapToILog(instruction.TargetMethod());

		public MethodReference TryFindGuard(MethodReference md) => guardProperties.TryGetValue(md.Name, out var retval) ? retval : null;
		public MethodReference TryFindGuard(Instruction instruction) => TryFindGuard(instruction.TargetMethod());

		private static Dictionary<string, MethodReference> GetILogMap(ModuleDefinition module, TypeDefinition logTo, TypeDefinition ilog)
		{
			var retval = new Dictionary<string, MethodReference>();

			foreach (var m in logTo.Methods)
			{
				var matching = FindMatching(m, ilog);
				if (matching == null)
					throw new InvalidOperationException($"Cannot find matching method in {ilog} for {m}");

				retval.Add(m.ToString(), module.ImportReference(matching));
			}

			return retval;
		}

		private static Dictionary<string, MethodReference> GetGuardProperties(ModuleDefinition module, TypeDefinition ilog)
		{
			var retval = new Dictionary<string, MethodReference>();

			foreach (var p in ilog.Properties)
			{
				if (!p.Name.StartsWith("Is") || !p.Name.EndsWith("Enabled"))
					continue;

				var getter = p.GetMethod;
				if (getter == null)
					throw new InvalidOperationException($"Property {p} does not have a getter");

				// IsXXXEnabled => keep only XXX, so we can look them up by the methods' name
				var name = p.Name.Substring(2, p.Name.Length - 9);
				retval.Add(name, module.ImportReference(getter));
			}

			return retval;
		}

		private static MethodDefinition FindMatching(MethodDefinition what, TypeDefinition where)
		{
			return where
					.Methods
					.FirstOrDefault(m => m.Name == what.Name
											&& CilComparer.AreSameSignature(what, m));
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
