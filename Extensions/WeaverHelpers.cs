using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Mono.Cecil;

namespace Enyim.Build
{
	public static class WeaverHelpers
	{
		public static IEnumerable<MethodDefinition> AllMethods(ModuleDefinition module) => module.Types.IncludeNestedTypes().SelectMany(t => t.Methods);

		public static IEnumerable<MethodDefinition> AllMethodsWithBody(ModuleDefinition module) => AllMethods(module).Where(m => m.HasBody);

		public static IEnumerable<MethodDefinition> AllMethodsWithBody(IEnumerable<TypeDefinition> types) => types.SelectMany(m => m.Methods).Where(m => m.HasBody);
	}
}
