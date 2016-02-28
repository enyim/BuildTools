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
		public static IEnumerable<TypeReference> TypesFromAnywhere(this ModuleDefinition module)
		{
			return module.Types.Concat(module.GetTypeReferences());
		}

		public static TypeAttributes Add(this TypeAttributes target, TypeAttributes value)
		{
			return target | value;
		}

		public static TypeAttributes Remove(this TypeAttributes target, TypeAttributes value)
		{
			return target & ~value;
		}

		public static void Append(this MethodDefinition method, IEnumerable<Instruction> code)
		{
			var instructions = method.Body.Instructions;

			foreach (var instruction in code)
				instructions.Add(instruction);
		}

		public static TypeReference FindType(this ModuleDefinition module, string fullName)
		{
			return module.Types.FirstOrDefault(t => t.FullName == fullName);
		}

		public static MethodReference FindConstructor(this TypeDefinition type, params TypeReference[] args)
		{
			return type.FindMethod(".ctor", args);
		}

		public static MethodReference FindConstructor(this TypeDefinition type, IEnumerable<TypeReference> args)
		{
			return type.FindMethod(".ctor", args);
		}

		public static MethodReference FindMethod(this TypeDefinition type, string name, params TypeReference[] args)
		{
			return type.FindMethod(name, args.AsEnumerable<TypeReference>());
		}

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

		public static CustomAttribute Clone(this CustomAttribute what)
		{
			var customAttribute = new CustomAttribute(what.Constructor);

			foreach (var arg in what.ConstructorArguments)
				customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(arg.Type, arg.Value));

			foreach (var prop in what.Properties)
				customAttribute.Properties.Add(new CustomAttributeNamedArgument(prop.Name, new CustomAttributeArgument(prop.Argument.Type, prop.Argument.Value)));

			foreach (var field in what.Fields)
				customAttribute.Fields.Add(new CustomAttributeNamedArgument(field.Name, new CustomAttributeArgument(field.Argument.Type, field.Argument.Value)));

			return customAttribute;
		}

		public static void CopyAttrsTo(this ICustomAttributeProvider source, ICustomAttributeProvider target)
		{
			foreach (var what in source.CustomAttributes)
				target.CustomAttributes.Add(what.Clone());
		}

		public static bool IsAttrDefined<T>(this ICustomAttributeProvider source)
		{
			return source.GetAttr<T>() != null;
		}

		public static CustomAttribute GetAttr<T>(this ICustomAttributeProvider source)
		{
			var expected = typeof(T).FullName;

			return source.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == expected);
		}

		public static CustomAttribute Named(this Collection<CustomAttribute> source, string name)
		{
			return source.FirstOrDefault(a => a.AttributeType.Name == name);
		}

		public static T Named<T>(this Collection<T> source, string name)
			where T : MemberReference
		{
			return source.FirstOrDefault(p => p.Name == name);
		}

		public static T GetPropertyValue<T>(this CustomAttribute source, string name)
		{
			var prop = source.Properties.Named(name);

			return prop.Name != null
					? (T)prop.Argument.Value
					: default(T);
		}

		public static void SetPropertyValue(this CustomAttribute source, string name, TypeReference type, object value)
		{
			var prop = source.Properties.Named(name);
			if (prop.Name != null)
				source.Properties.Remove(prop);

			source.Properties.Add(new CustomAttributeNamedArgument(name, new CustomAttributeArgument(type, value)));
		}

		public static CustomAttributeNamedArgument Named(this Collection<CustomAttributeNamedArgument> source, string name)
		{
			return source.FirstOrDefault(cana => cana.Name == name);
		}

		public static bool TryGetPropertyValue<T>(this CustomAttribute source, string name, out T value)
		{
			var prop = source.Properties.Named(name);

			if (prop.Name == null)
			{
				value = default(T);
				return false;
			}

			value = (T)prop.Argument.Value;

			return true;
		}

		public static Instruction AsLdc_I(this int i)
		{
			return Instruction.Create(OpCodes.Ldc_I4, i);
		}

		public static IEnumerable<T> Once<T>(this T item)
		{
			return new T[1] { item };
		}

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

		public static MethodDefinition NewCtor(ModuleDefinition module, TypeDefinition target)
		{
			var retval = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);

			var body = retval.Body.Instructions;
			body.Add(Instruction.Create(OpCodes.Ldarg_0));
			body.Add(Instruction.Create(OpCodes.Call, module.Import(target.BaseType.Resolve().FindConstructor())));
			body.Add(Instruction.Create(OpCodes.Ret));

			target.Methods.Add(retval);

			return retval;
		}

		public static IEnumerable<Instruction> GetOpsOf(this MethodDefinition method, params OpCode[] ops)
		{
			var tmp = new HashSet<OpCode>(ops);

			return method.Body.Instructions.Where(i => tmp.Contains(i.OpCode));
		}

		public static void Add<T>(this Collection<T> source, IEnumerable<T> what)
		{
			foreach (var obj in what)
				source.Add(obj);
		}

		public static void Add<T>(this Collection<T> source, params T[] what)
		{
			foreach (var obj in what)
				source.Add(obj);
		}

		public static void Remove<T>(this Collection<T> source, IEnumerable<T> what)
		{
			foreach (var obj in what)
				source.Remove(obj);
		}

		public static MethodReference ImportInto(this MethodReference method, ModuleDefinition module)
		{
			return module.Import(method);
		}

		public static TypeReference ImportInto(this TypeReference type, ModuleDefinition module)
		{
			return module.Import(type);
		}

		public static CustomAttribute AddAttr<TAttribute>(this ICustomAttributeProvider self, ModuleDefinition module, params object[] ctorArgs) where TAttribute : Attribute
		{
			var retval = module.NewAttr(module.Import(typeof(TAttribute)), ctorArgs);

			self.CustomAttributes.Add(retval);

			return retval;
		}

		public static CustomAttribute NewAttr(this ModuleDefinition module, Type attrType, params object[] ctorArgs)
		{
			return module.NewAttr(module.Import(attrType), ctorArgs);
		}

		public static CustomAttribute NewAttr(this ModuleDefinition module, TypeReference attrRef, params object[] ctorArgs)
		{
			var argTypes = ctorArgs.Select(o => o != null ? module.Import(o.GetType()) : module.TypeSystem.Object).ToArray();
			var retval = new CustomAttribute(module.Import(attrRef.Resolve().FindConstructor(argTypes)));

			for (var i = 0; i < ctorArgs.Length; i++)
				retval.ConstructorArguments.Add(new CustomAttributeArgument(argTypes[i], ctorArgs[i]));

			return retval;
		}

		public static FieldDefinition DeclareStaticField(this TypeDefinition typeDef, ModuleDefinition module, TypeReference fieldType, string name, Func<IEnumerable<Instruction>> init, FieldAttributes attributes = FieldAttributes.Private)
		{
			var retval = new FieldDefinition(name, FieldAttributes.Static | FieldAttributes.InitOnly | attributes, fieldType);
			retval.AddAttr<CompilerGeneratedAttribute>(module);
			typeDef.Fields.Insert(0, retval);

			var cctor = typeDef.Methods.FirstOrDefault(m => m.IsConstructor && m.IsStatic);

			if (cctor == null)
			{
				cctor = new MethodDefinition(".cctor", MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, module.TypeSystem.Void);
				typeDef.Methods.Insert(0, cctor);
				cctor.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
				cctor.AddAttr<CompilerGeneratedAttribute>(module);
			}

			var body = cctor.Body.Instructions;
			var index = 0;

			foreach (Instruction instruction in init())
				body.Insert(index++, instruction);

			body.Insert(index, Instruction.Create(OpCodes.Stsfld, retval));

			return retval;
		}

		//public static TypeReference GetEnumBaseType(this TypeDefinition self)
		//{
		//	return self.Fields.First(f => f.Name == "value__").FieldType;
		//}
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
