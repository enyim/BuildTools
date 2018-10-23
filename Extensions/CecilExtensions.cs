using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Enyim.Build
{
	public static class CecilExtensions
	{
		public static IEnumerable<TypeReference> IncludeReferencedTypes(this ModuleDefinition module) => module.Types.Concat(module.GetTypeReferences());

		public static TypeAttributes Add(this TypeAttributes target, TypeAttributes value) => target | value;

		public static TypeAttributes Remove(this TypeAttributes target, TypeAttributes value) => target & ~value;

		public static void Append(this MethodDefinition method, IEnumerable<Instruction> code)
		{
			var instructions = method.Body.Instructions;

			foreach (var instruction in code)
				instructions.Add(instruction);
		}

		public static TypeReference FindType(this ModuleDefinition module, string fullName) => module.Types.FirstOrDefault(t => t.FullName == fullName);

		public static MethodReference FindConstructor(this TypeDefinition type, params TypeReference[] args) => type.FindMethod(".ctor", args);

		public static MethodReference FindConstructor(this TypeDefinition type, IEnumerable<TypeReference> args) => type.FindMethod(".ctor", args);

		public static MethodReference FindMethod(this TypeDefinition type, string name, params TypeReference[] args) => type.FindMethod(name, args.AsEnumerable<TypeReference>());

		public static MethodReference FindMethod(this TypeDefinition type, string name, IEnumerable<TypeReference> args)
		{
			var expected = args.Select(t => t.FullName).ToArray();

			return SelfAndBase(type)
						.SelectMany(t => t.Methods)
						.FirstOrDefault(m => m.Name == name
												&& expected.SequenceEqual(m.Parameters.Select(p => p.ParameterType.FullName)));
		}

		private static IEnumerable<TypeDefinition> SelfAndBase(TypeDefinition start)
		{
			while (true)
			{
				yield return start;

				var baseType = start.BaseType;
				if (baseType != start && baseType != null)
					start = baseType.Resolve();
				else
					break;
			}
		}

		public static T Named<T>(this Collection<T> source, string name) where T : MemberReference
		{
			if (source != null)
			{
				for (int i = 0, max = source.Count; i < max; i++)
				{
					var current = source[i];
					if (current.Name == name)
						return current;
				}
			}

			return null;
		}

		public static T Named<T>(this IEnumerable<T> source, string name) where T : MemberReference => source.FirstOrDefault(p => p.Name == name);

		public static TypeDefinition NewType(this ModuleDefinition module, string @namespace, string name, TypeReference baseType = null, TypeAttributes attributes = TypeAttributes.Public)
		{
			var retval = new TypeDefinition(@namespace, name, TypeAttributes.BeforeFieldInit | attributes, baseType ?? module.TypeSystem.Object);

			if ((attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed))
				NewCtor(module, retval);

			return retval;
		}

		public static TypeDefinition NewType(this ModuleDefinition module, TypeDefinition parent, string name, TypeReference baseType = null, TypeAttributes attributes = TypeAttributes.Public)
		{
			var target = new TypeDefinition(parent.Name, name, TypeAttributes.BeforeFieldInit | attributes, baseType ?? module.TypeSystem.Object);
			if ((attributes & (TypeAttributes.Abstract | TypeAttributes.Sealed)) != (TypeAttributes.Abstract | TypeAttributes.Sealed))
				NewCtor(module, target);

			parent.NestedTypes.Add(target);

			return target;
		}

		public static MethodDefinition NewCtor(this ModuleDefinition module, TypeDefinition target)
		{
			var retval = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);

			var body = retval.Body.Instructions;
			body.Add(Instruction.Create(OpCodes.Ldarg_0));
			body.Add(Instruction.Create(OpCodes.Call, module.ImportReference(target.BaseType.Resolve().FindConstructor())));
			body.Add(Instruction.Create(OpCodes.Ret));

			target.Methods.Add(retval);

			return retval;
		}

		public static IEnumerable<Instruction> GetOpsOf(this MethodDefinition method, params OpCode[] ops)
		{
			var tmp = new HashSet<OpCode>(ops);

			return method.Body.Instructions.Where(i => tmp.Contains(i.OpCode));
		}

		public static bool Is(this Instruction self, params OpCode[] ops)
		{
			foreach (var op in ops)
			{
				if (self.OpCode == op)
					return true;
			}

			return false;
		}

		public static MethodReference ImportInto(this MethodReference method, ModuleDefinition module) => module.ImportReference(method);

		public static TypeReference ImportInto(this TypeReference type, ModuleDefinition module) => module.ImportReference(type);

		public static MethodDefinition GetOrCreateStaticConstructor(this TypeDefinition typeDef)
		{
			var cctor = typeDef.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);

			if (cctor == null)
			{
				var module = typeDef.Module;

				cctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
				cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
				cctor.AddAttr<CompilerGeneratedAttribute>(module);

				typeDef.Methods.Add(cctor);
			}

			return cctor;
		}

		public static FieldDefinition DeclareStaticField(this TypeDefinition typeDef, ModuleDefinition module, TypeReference fieldType, string name, Func<FieldDefinition, IEnumerable<Instruction>> init, FieldAttributes attributes = FieldAttributes.Private)
		{
			var field = new FieldDefinition(name, FieldAttributes.Static | FieldAttributes.InitOnly | attributes, fieldType);
			field.AddAttr<CompilerGeneratedAttribute>(module);

			typeDef
				.Fields
				.Add(field);

			typeDef
				.GetOrCreateStaticConstructor()
				.Body.Instructions
					.Add(init(field));

			return field;
		}

		public static IEnumerable<TypeDefinition> IncludeNestedTypes(this IEnumerable<TypeDefinition> source) => source.SelectMany(SelfAndNested);

		private static IEnumerable<TypeDefinition> SelfAndNested(TypeDefinition type) => new[] { type }.Concat(type.NestedTypes.SelectMany(SelfAndNested));

		public static MethodReference TargetMethod(this Instruction self) => (MethodReference)self.Operand;
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
