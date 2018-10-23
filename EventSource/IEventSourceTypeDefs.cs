using System;
using Mono.Cecil;

namespace Enyim.Build.Rewriters.EventSource
{
	internal interface IEventSourceTypeDefs
	{
		TypeReference BaseTypeRef { get; }
		TypeDefinition BaseTypeImpl { get; }

		TypeReference EventLevel { get; }
		TypeReference EventKeywords { get; }
		TypeReference EventOpcode { get; }
		TypeReference EventTask { get; }

		TypeReference EventSourceAttribute { get; }
		TypeReference EventAttribute { get; }
		TypeReference NonEventAttribute { get; }

		MethodReference IsEnabledSpecific { get; }
		MethodReference IsEnabledFallback { get; }
		MethodReference WriteEventFallback { get; }
		MethodReference WriteEventCore { get; }

		TypeReference EventDataRef { get; }
		TypeDefinition EventDataImpl { get; }
		MethodReference EventDataSetDataPointer { get; }
		MethodReference EventDataSetSize { get; }
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
