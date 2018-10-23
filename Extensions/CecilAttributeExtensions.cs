using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Enyim.Build
{
	public static class CecilAttributeExtensions
	{
		public static CustomAttribute Clone(this CustomAttribute what)
		{
			var customAttribute = new CustomAttribute(what.Constructor);

			customAttribute.ConstructorArguments.Add(what.ConstructorArguments.Select(arg => new CustomAttributeArgument(arg.Type, arg.Value)));
			customAttribute.Properties.Add(what.Properties.Select(prop => new CustomAttributeNamedArgument(prop.Name, new CustomAttributeArgument(prop.Argument.Type, prop.Argument.Value))));
			customAttribute.Fields.Add(what.Fields.Select(field => new CustomAttributeNamedArgument(field.Name, new CustomAttributeArgument(field.Argument.Type, field.Argument.Value))));

			return customAttribute;
		}

		public static void CopyAttrsTo(this ICustomAttributeProvider source, ICustomAttributeProvider target)
		{
			target.CustomAttributes.Add(source.CustomAttributes.Select(what => what.Clone()));
		}

		public static bool IsDefined<T>(this ICustomAttributeProvider source) where T : Attribute => source.Typed<T>() != null;
		public static CustomAttribute Typed<T>(this ICustomAttributeProvider source) where T : Attribute => source.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(T).FullName);
		public static CustomAttribute Named(this Collection<CustomAttribute> source, string name) => source.FirstOrDefault(a => a.AttributeType.Name == name);
		public static CustomAttributeNamedArgument Named(this Collection<CustomAttributeNamedArgument> source, string name) => source.FirstOrDefault(cana => cana.Name == name);

		public static T GetPropertyValue<T>(this CustomAttribute source, string name) => source.TryGetPropertyValue<T>(name, out var retval) ? retval : default;

		public static void SetPropertyValue(this CustomAttribute source, string name, TypeReference type, object value)
		{
			var prop = source.Properties.Named(name);
			if (prop.Name != null)
				source.Properties.Remove(prop);

			source.Properties.Add(new CustomAttributeNamedArgument(name, new CustomAttributeArgument(type, value)));
		}

		public static bool TryGetPropertyValue<T>(this CustomAttribute source, string name, out T value)
		{
			var prop = source.Properties.Named(name);

			if (prop.Name == null)
			{
				value = default;
				return false;
			}

			value = (T)prop.Argument.Value;

			return true;
		}

		public static CustomAttribute AddAttr<TAttribute>(this ICustomAttributeProvider self, ModuleDefinition module, params object[] ctorArgs) where TAttribute : Attribute
		{
			var retval = module.NewAttr(module.ImportReference(typeof(TAttribute)), ctorArgs);

			self.CustomAttributes.Add(retval);

			return retval;
		}

		public static CustomAttribute NewAttr(this ModuleDefinition module, Type attrType, params object[] ctorArgs) => module.NewAttr(module.ImportReference(attrType), ctorArgs);

		public static CustomAttribute NewAttr(this ModuleDefinition module, TypeReference attrRef, params object[] ctorArgs)
		{
			var argTypes = ctorArgs.Select(o => o != null ? module.ImportReference(o.GetType()) : module.TypeSystem.Object).ToArray();
			var retval = new CustomAttribute(module.ImportReference(attrRef.Resolve().FindConstructor(argTypes)));

			for (var i = 0; i < ctorArgs.Length; i++)
				retval.ConstructorArguments.Add(new CustomAttributeArgument(argTypes[i], ctorArgs[i]));

			return retval;
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
