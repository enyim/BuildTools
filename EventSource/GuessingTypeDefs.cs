using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Rewriters.EventSource
{
	internal class GuessingTypeDefs : IEventSourceTypeDefs
	{
		public GuessingTypeDefs(ModuleDefinition module)
		{
			BaseTypeRef = GuessBaseType(module).ImportInto(module);
			BaseTypeImpl = BaseTypeRef.Resolve();

			var sourceModule = BaseTypeImpl.Module;

			EventLevel = ImportOne(module, sourceModule, "EventLevel", "Microsoft.Diagnostics.Tracing", "System.Diagnostics.Tracing");
			EventKeywords = ImportOne(module, sourceModule, "EventKeywords", "Microsoft.Diagnostics.Tracing", "System.Diagnostics.Tracing");
			EventOpcode = ImportOne(module, sourceModule, "EventOpcode", "Microsoft.Diagnostics.Tracing", "System.Diagnostics.Tracing");
			EventTask = ImportOne(module, sourceModule, "EventTask", "Microsoft.Diagnostics.Tracing", "System.Diagnostics.Tracing");
			EventSourceAttribute = ImportOne(module, sourceModule, "EventSourceAttribute", "Microsoft.Diagnostics.Tracing", "System.Diagnostics.Tracing");
			EventAttribute = ImportOne(module, sourceModule, "EventAttribute", "Microsoft.Diagnostics.Tracing", "System.Diagnostics.Tracing");
			NonEventAttribute = ImportOne(module, sourceModule, "NonEventAttribute", "Microsoft.Diagnostics.Tracing", "System.Diagnostics.Tracing");

			IsEnabledSpecific = BaseTypeImpl.FindMethod("IsEnabled", EventLevel, EventKeywords).ImportInto(module);
			IsEnabledFallback = BaseTypeImpl.FindMethod("IsEnabled").ImportInto(module);
			WriteEventFallback = BaseTypeImpl.FindMethod("WriteEvent", module.TypeSystem.Int32, module.ImportReference(typeof(object[]))).ImportInto(module);
			WriteEventCore = BaseTypeImpl.Methods.First(m => m.Name == "WriteEventCore").ImportInto(module);

			EventDataRef = BaseTypeImpl.NestedTypes.First(t => t.Name == "EventData").ImportInto(module);
			EventDataImpl = EventDataRef.Resolve();
			EventDataSetDataPointer = EventDataImpl.Properties.Named("DataPointer").SetMethod.ImportInto(module);
			EventDataSetSize = EventDataImpl.Properties.Named("Size").SetMethod.ImportInto(module);
		}

		public TypeReference BaseTypeRef { get; }
		public TypeDefinition BaseTypeImpl { get; }

		public TypeReference EventLevel { get; }
		public TypeReference EventKeywords { get; }
		public TypeReference EventOpcode { get; }
		public TypeReference EventTask { get; }

		public TypeReference EventSourceAttribute { get; }
		public TypeReference EventAttribute { get; }
		public TypeReference NonEventAttribute { get; }

		public MethodReference IsEnabledSpecific { get; }
		public MethodReference IsEnabledFallback { get; }
		public MethodReference WriteEventFallback { get; }
		public MethodReference WriteEventCore { get; }

		public TypeReference EventDataRef { get; }
		public TypeDefinition EventDataImpl { get; }
		public MethodReference EventDataSetDataPointer { get; }
		public MethodReference EventDataSetSize { get; }

		private static TypeReference GuessBaseType(ModuleDefinition module)
		{
			var name = module.AssemblyReferences.FirstOrDefault(r => r.Name == "Microsoft.Diagnostics.Tracing.EventSource");
			if (name != null)
			{
				var def = module.AssemblyResolver
											.Resolve(name)
											.Modules
											.SelectMany(m => m.Types)
											.Named("Microsoft.Diagnostics.Tracing.EventSource");

				if (def != null)
					return module.ImportReference(def);
			}

			return module.ImportReference(typeof(System.Diagnostics.Tracing.EventSource));
		}

		private static TypeReference ImportOne(ModuleDefinition target, ModuleDefinition source, string name, params string[] namespaces)
			=> namespaces
					.Select(n => source.FindType(n + "." + name))
					.First(t => t != null)
					.ImportInto(target);
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
