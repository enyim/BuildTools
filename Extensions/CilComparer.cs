using System;
using System.Linq;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Enyim.Build
{
	/// <summary>
	/// Code is based on Mono.Cecil.MetadataResolver
	/// https://github.com/jbevain/cecil/blob/master/Mono.Cecil/MetadataResolver.cs
	///
	/// Author:
	///   Jb Evain (jbevain@gmail.com)
	///
	/// Copyright (c) 2008 - 2015 Jb Evain
	/// Copyright (c) 2008 - 2011 Novell, Inc.
	///
	/// Licensed under the MIT/X11 license.
	///
	/// </summary>
	public static class CilComparer
	{
		public static bool AreSame(TypeDefinition a, TypeDefinition b) => a.FullName == b.FullName;

		public static bool AreSame(TypeReference a, TypeReference b)
		{
			if (a == b) return true;
			if (a == null || b == null) return false;
			if (a.MetadataType != b.MetadataType) return false;

			if (a.IsGenericParameter)
			{
				return b.IsGenericParameter
						&& ((GenericParameter)a).Position == ((GenericParameter)b).Position;
			}

			if (a is TypeSpecification tsa)
				return AreSame(tsa, b as TypeSpecification);

			if (a.Name != b.Name || a.Namespace != b.Namespace)
				return false;

			return AreSame(a.DeclaringType, b.DeclaringType);
		}

		public static bool AreSameSignature(MethodReference a, MethodReference b)
		{
			if (a.Name != b.Name) return false;

			return AreSame(a.Parameters, b.Parameters);
		}

		private static bool AreSame(Collection<ParameterDefinition> a, Collection<ParameterDefinition> b)
		{
			var count = a.Count;
			if (count == 0) return true;
			if (count != b.Count) return false;

			for (var i = 0; i < count; i++)
			{
				if (!AreSame(a[i].ParameterType, b[i].ParameterType))
					return false;
			}

			return true;
		}

		private static bool AreSame(TypeSpecification a, TypeSpecification b)
		{
			if (!AreSame(a.ElementType, b.ElementType))
				return false;

			if (a.IsGenericInstance)
				return AreSame((GenericInstanceType)a, (GenericInstanceType)b);

			if (a.IsRequiredModifier || a.IsOptionalModifier)
				return AreSame((IModifierType)a, (IModifierType)b);

			if (a.IsArray)
				return b.IsArray && ((ArrayType)a).Rank == ((ArrayType)b).Rank;

			return true;
		}

		private static bool AreSame(GenericInstanceType a, GenericInstanceType b)
		{
			if (a.GenericArguments.Count != b.GenericArguments.Count)
			{
				return false;
			}
			for (var i = 0; i < a.GenericArguments.Count; i++)
			{
				if (!AreSame(a.GenericArguments[i], b.GenericArguments[i]))
				{
					return false;
				}
			}
			return true;
		}

		private static bool AreSame(IModifierType a, IModifierType b) => AreSame(a.ModifierType, b.ModifierType);
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
