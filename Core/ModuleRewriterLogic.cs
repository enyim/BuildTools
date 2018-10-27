using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build
{
	internal class ModuleRewriterLogic
	{
		private readonly IModuleVisitor[] visitors;

		public ModuleRewriterLogic(IModuleVisitor[] visitors)
		{
			this.visitors = visitors;
		}

		public void Execute(ModuleDefinition module)
		{
			foreach (var visitor in visitors)
			{
				if (visitor.Enabled) visitor.BeforeModule(module);
			}

			DoTypes(module.Types);

			foreach (var visitor in visitors)
			{
				if (visitor.Enabled) visitor.AfterModule(module);
			}
		}

		private void DoTypes(Mono.Collections.Generic.Collection<TypeDefinition> types)
		{
			var seen = new HashSet<string>();

			foreach (var type in CollectChained(types, (v, t) => v.BeforeType(t)))
			{
				if (type.Name == "<Module>") continue;

				Debug.Assert(seen.Add(type.FullName));

				if (type.HasFields) RunChained(type.Fields, (v, f) => v.BeforeField(f));
				if (type.HasProperties) RunChained(type.Properties, (v, p) => v.BeforeProperty(p));
				if (type.HasMethods) DoMethods(type);

				if (type.NestedTypes.Count > 0) DoTypes(type.NestedTypes);

				RunVisitors(type.Fields, (v, f) => v.AfterField(f));
				RunVisitors(type.Properties, (v, p) => v.AfterProperty(p));
				RunVisitors(type.Methods, (v, m) => v.AfterMethod(m));
			}

			RunVisitors(types, (v, t) => v.AfterType(t));
		}

		private void DoMethods(TypeDefinition type)
		{
			foreach (var method in CollectChained(type.Methods, (mv, m) => mv.BeforeMethod(m)))
			{
				if (method.HasBody)
					RunChained(method.Body.Instructions, (bv, i) => bv.MethodInstruction(method, i));
			}
		}

		protected IEnumerable<T> CollectChained<T>(Mono.Collections.Generic.Collection<T> collection, Func<IModuleVisitor, T, T> action)
			where T : class
		{
			var index = 0;

			while (index < collection.Count)
			{
				var old = collection[index];
				var @new = old;

				foreach (var visitor in visitors)
				{
					if (visitor.Enabled)
					{
						@new = action(visitor, @new);
						if (@new == null) break;
					}
				}

				if (@new != old)
				{
					collection.RemoveAt(index);
					if (@new == null) continue;

					collection.Insert(index, @new);
				}

				yield return @new;

				index++;
			}
		}

		protected void RunChained<T>(Mono.Collections.Generic.Collection<T> collection, Func<IModuleVisitor, T, T> action)
			where T : class
		{
			var index = 0;

			while (index < collection.Count)
			{
				var old = collection[index];
				var @new = old;

				foreach (var visitor in visitors)
				{
					if (visitor.Enabled)
					{
						@new = action(visitor, @new);
						if (@new == null) break;
					}
				}

				if (@new != old)
				{
					collection.RemoveAt(index);
					if (@new == null) continue;

					collection.Insert(index, @new);
				}

				index++;
			}
		}

		protected void RunVisitors<T>(Mono.Collections.Generic.Collection<T> collection, Action<IModuleVisitor, T> action)
			where T : class
		{
			var max = collection.Count;

			for (var i = 0; i < max; i++)
			{
				var current = collection[i];

				foreach (var visitor in visitors)
				{
					if (visitor.Enabled)
					{
						action(visitor, current);
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
