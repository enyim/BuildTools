using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	public class ModuleWeaver : ModuleWeaverBase
	{
		protected override void OnExecute()
		{
			Log.Info = LogInfo;
			Log.Warn = LogWarning;
			Log.Error = LogError;

			var types = ModuleDefinition.Types.IncludeNestedTypes().ToArray();
			var implemented = ImplementAbstracts(types)
								.Concat(ImplementStatic(types))
								.Concat(ImplementInterfaces(types))
								.ToArray();

			var plugins = from t in typeof(ModuleWeaver).Assembly.GetTypes()
						  where t.IsClass && typeof(IProcessEventSources).IsAssignableFrom(t)
						  orderby t.GetCustomAttribute<OrderAttribute>()?.Order
						  select t;

			foreach (var p in plugins)
				((IProcessEventSources)Activator.CreateInstance(p)).Rewrite(ModuleDefinition, implemented);
		}

		private IEnumerable<ImplementedEventSource> ImplementAbstracts(TypeDefinition[] types)
		{
			var builder = new AbstractEventSourceBuilder(ModuleDefinition);

			foreach (var t in types)
				if (t.IsAbstract && t.BaseType?.Name == "EventSource")
					yield return builder.Implement(t);
		}

		private IEnumerable<ImplementedEventSource> ImplementStatic(TypeDefinition[] types)
		{
			var builder = new StaticEventSourceBuilder(ModuleDefinition);

			foreach (var t in types)
				if (t.IsAbstract
						&& t.IsSealed
						&& t.CustomAttributes.Named("EventSourceAttribute") != null)
					yield return builder.Implement(t);
		}

		private IEnumerable<ImplementedEventSource> ImplementInterfaces(TypeDefinition[] types)
		{
			var builder = new InterfaceBasedEventSourceBuilder(ModuleDefinition);

			foreach (var t in types)
			{
				if (t.IsInterface)
				{
					var a = t.CustomAttributes.Named("AsEventSourceAttribute");

					if (a != null)
						yield return builder.Implement(a.GetPropertyValue<string>("Name"), a.GetPropertyValue<string>("Guid"), t);
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
