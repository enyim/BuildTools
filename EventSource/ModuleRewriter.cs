using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Rewriters.EventSource
{
	public class ModuleRewriter : ModuleRewriterBase
	{
		private ImplementedEventSource[] implemented;

		protected override void OnExecute(ModuleDefinition module)
		{
			var types = module.Types.IncludeNestedTypes().ToArray();
			implemented = ImplementAbstracts(module, types)
							.Concat(ImplementStatic(module, types))
							.Concat(ImplementInterfaces(module, types))
							.ToArray();

			base.OnExecute(module);
		}

		protected override IModuleVisitor[] GetVisitors()
			=> new IModuleVisitor[]
			{
				new AddImplementedTypes(implemented),

				new FactoryImplementer(implemented),
				new IsEnabledRewriter(implemented),

				new CreateInstanceFieldsForStaticTemplates(implemented),

				new FixStaticCalls(implemented),
				new RewriteEventSourceCalls(implemented),

				new RewriteProps(implemented),
				new RewriteFields(implemented),

				new OptimizeImplementedMethods(implemented),
				new RemoveStaticTemplates(implemented),
				new RemoveInterfaceTemplates(implemented),
				new RemoveBuildAssemblyReferences(implemented),
			};

		private static IEnumerable<ImplementedEventSource> ImplementAbstracts(ModuleDefinition module, TypeDefinition[] types)
		{
			var builder = new AbstractEventSourceBuilder(module);

			foreach (var type in types)
			{
				if (type.IsAbstract && type.BaseType?.Name == "EventSource")
					yield return builder.Implement(type);
			}
		}

		private static IEnumerable<ImplementedEventSource> ImplementStatic(ModuleDefinition module, TypeDefinition[] types)
		{
			var builder = new StaticEventSourceBuilder(module);

			foreach (var type in types)
			{
				if (type.IsAbstract
						&& type.IsSealed
						&& type.CustomAttributes.Named("EventSourceAttribute") != null)
				{
					yield return builder.Implement(type);
				}
			}
		}

		private static IEnumerable<ImplementedEventSource> ImplementInterfaces(ModuleDefinition module, TypeDefinition[] types)
		{
			var builder = new InterfaceBasedEventSourceBuilder(module);

			foreach (var type in types)
			{
				if (type.IsInterface)
				{
					var a = type.CustomAttributes.Named("AsEventSourceAttribute");

					if (a != null)
						yield return builder.Implement(a.GetPropertyValue<string>("Name"), a.GetPropertyValue<string>("Guid"), type);
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
