using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Weavers.LogTo
{
	using BindingFlags = System.Reflection.BindingFlags;

	internal class LogDefinition
	{
		private readonly ModuleDefinition module;
		private readonly Dictionary<string, MethodReference> ilogMap;
		private readonly Dictionary<string, MethodReference> guardProperties;
		private readonly TypeDefinition logTo;
		private readonly TypeDefinition ilog;
		private readonly MethodReference getTypeFromHandle;

		public LogDefinition(ModuleDefinition module)
		{
			this.module = module;

			logTo = ResolveByName("LogTo", false);
			ilog = ResolveByName("ILog", false);

			if (logTo == null || ilog == null) return;

			ilogMap = GetILogMap(module, logTo, ilog);
			guardProperties = GetGuardProperties(module, ilog);
			getTypeFromHandle = module.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));

			IsValid = true;
		}

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

				var name = p.Name.Substring(2, p.Name.Length - 9);
				retval.Add(name, module.ImportReference(getter));
			}

			return retval;
		}

		public bool IsValid { get; }

		private TypeDefinition ResolveByName(string name, bool shouldThrow = true)
		{
			var modules = new List<ModuleDefinition> { module };
			if (ilog != null) modules.Add(ilog.Module);
			if (logTo != null) modules.Add(logTo.Module);

			var type = modules.SelectMany(m => m.IncludeReferencedTypes()).FirstOrDefault(t => t.Name == name);
			if (type != null)
				return module.ImportReference(type).Resolve();

			if (shouldThrow)
				throw new InvalidOperationException($"Could not find type reference {name}");

			return null;
		}

		public FieldDefinition DeclareLogger(TypeDefinition typeDef)
		{
			if (typeDef.Interfaces.Any(t => t.InterfaceType.Name == "IAsyncStateMachine"))
				typeDef = typeDef.DeclaringType;

			var retval = typeDef.Fields.FirstOrDefault(f => f.Name == "<>log");
			if (retval != null)
				return retval;

			return typeDef.DeclareStaticField(this.module, module.ImportReference(ilog), "<>log", () =>
			{
				var factory = module.ImportReference(ResolveByName("LogManager", true).Methods.First(m => m.Name == "GetLogger"));

				return new[]
				{
					Instruction.Create(OpCodes.Ldtoken, typeDef),
					Instruction.Create(OpCodes.Call, getTypeFromHandle),
					Instruction.Create(OpCodes.Call, factory)
				};
			}, FieldAttributes.Private);
		}

		public MethodReference MapToILog(MethodReference md)
		{
			return ilogMap[md.ToString()];
		}

		public MethodReference MapToILog(Instruction instruction)
		{
			return MapToILog((MethodReference)instruction.Operand);
		}

		public MethodReference FindGuard(MethodReference md)
		{
			MethodReference retval;

			if (!guardProperties.TryGetValue(md.Name, out retval))
				throw new InvalidOperationException($"Method {md} does not have a guard property defined in {ilog}");

			return retval;
		}

		public MethodReference FindGuard(Instruction instruction)
		{
			return FindGuard((MethodReference)instruction.Operand);
		}

		private static MethodDefinition FindMatching(MethodDefinition what, TypeDefinition where)
		{
			var find = LogDefinition.GetParams(what).ToArray();

			return where.Methods.FirstOrDefault(m => (m.Name == what.Name) && LogDefinition.GetParams(m).SequenceEqual(find));
		}

		public static bool IsLogger(Instruction i)
		{
			return (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt)
					&& ((MemberReference)i.Operand).DeclaringType.Name == "LogTo";
		}

		public static bool IsLogger(MethodReference m)
		{
			return m.DeclaringType.Name == "LogTo";
		}

		private static IEnumerable<string> GetParams(MethodDefinition md)
		{
			return md.Parameters.Select(p => p.ParameterType.FullName);
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
