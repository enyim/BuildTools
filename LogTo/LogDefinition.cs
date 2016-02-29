using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Enyim.Build.Weavers.LogTo
{
	internal class LogDefinition
	{
		private readonly ModuleDefinition module;
		private readonly Dictionary<string, MethodReference> ilogMap;
		private readonly Dictionary<string, MethodReference> guardMap;
		private readonly TypeDefinition logTo;
		private readonly TypeDefinition ilog;

		public LogDefinition(ModuleDefinition module)
		{
			this.module = module;

			logTo = ResolveByName("LogTo", false);
			ilog = ResolveByName("ILog", false);

			if (logTo == null || ilog == null) return;

			ilogMap = logTo.Methods.ToDictionary(m => m.ToString(), m => module.Import(FindMatching(m, ilog)));
			guardMap = ilog.Properties.ToDictionary(p => p.Name.Substring(2).Replace("Enabled", ""), p => module.Import(p.GetMethod));

			IsValid = true;
		}

		public bool IsValid { get; }

		private TypeDefinition ResolveByName(string name, bool shouldThrow = true)
		{
			var modules = new List<ModuleDefinition> { module };
			if (ilog != null) modules.Add(ilog.Module);
			if (logTo != null) modules.Add(logTo.Module);

			var type = modules.SelectMany(m => m.IncludeReferencedTypes()).FirstOrDefault(t => t.Name == name);
			if (type != null)
				return module.Import(type).Resolve();

			if (shouldThrow)
				throw new InvalidOperationException($"Could not find type reference {name}");

			return null;
		}

		public FieldDefinition DeclareLogger(TypeDefinition typeDef)
		{
			return typeDef.DeclareStaticField(this.module, module.Import(ilog), "<>log", () =>
			{
				var factory = module.Import(ResolveByName("LogManager", true).FindMethod("GetCurrentClassLogger"));

				return new[] { Instruction.Create(OpCodes.Call, factory) };
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
			return guardMap[md.Name];
		}

		public MethodReference FindGuard(Instruction instruction)
		{
			return FindGuard((MethodReference)instruction.Operand);
		}

		private MethodDefinition FindMatching(MethodDefinition what, TypeDefinition where)
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
