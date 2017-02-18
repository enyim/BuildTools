using System;
using System.Linq;
using Mono.Cecil;

namespace Enyim.Build.Weavers.EventSource
{
	internal class BaseClassTypeDefs : IEventSourceTypeDefs
	{
		public BaseClassTypeDefs(TypeDefinition type)
		{
			if (type.BaseType.Name != "EventSource")
				throw new InvalidOperationException("Base type must be EventSource");

			var ns = type.BaseType.Namespace + ".";
			var module = type.Module;

			BaseTypeRef = type.BaseType;
			BaseTypeImpl = type.BaseType.Resolve();

			var sourceModule = BaseTypeImpl.Module;
			EventLevel = sourceModule.FindType(ns + "EventLevel").ImportInto(module);
			EventKeywords = sourceModule.FindType(ns + "EventKeywords").ImportInto(module);
			EventOpcode = sourceModule.FindType(ns + "EventOpcode").ImportInto(module);
			EventTask = sourceModule.FindType(ns + "EventTask").ImportInto(module);

			EventSourceAttribute = sourceModule.FindType(ns + "EventSourceAttribute").ImportInto(module);
			EventAttribute = sourceModule.FindType(ns + "EventAttribute").ImportInto(module);
			NonEventAttribute = sourceModule.FindType(ns + "NonEventAttribute").ImportInto(module);

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
