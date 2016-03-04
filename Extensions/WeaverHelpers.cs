using System;
using System.Linq;
using System.Collections.Generic;
using Mono.Cecil;
using System.Xml.Linq;
using System.ComponentModel;
using System.Reflection;

namespace Enyim.Build
{
	public static class WeaverHelpers
	{
		public static IEnumerable<MethodDefinition> AllMethods(ModuleDefinition module)
		{
			return module.Types.IncludeNestedTypes().SelectMany(t => t.Methods);
		}

		public static IEnumerable<MethodDefinition> AllMethodsWithBody(ModuleDefinition module)
		{
			return AllMethods(module).Where(m => m.HasBody);
		}

		public static IEnumerable<MethodDefinition> AllMethodsWithBody(IEnumerable<TypeDefinition> types)
		{
			return types.SelectMany(m => m.Methods).Where(m => m.HasBody);
		}
	}

}
